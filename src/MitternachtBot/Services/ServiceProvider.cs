using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NLog;

namespace Mitternacht.Services {
	public interface INServiceProvider : IServiceProvider, IEnumerable<object> {
		ImmutableDictionary<Type, object> Services { get; }
		
		T GetService<T>();
	}

	public class NServiceProvider : INServiceProvider {
		public class ServiceProviderBuilder {
			private readonly ConcurrentDictionary<Type, object> _typeInstances = new ConcurrentDictionary<Type, object>();
			private readonly Logger                             _log;

			public ServiceProviderBuilder() {
				_log = LogManager.GetCurrentClassLogger();
			}

			public ServiceProviderBuilder AddManual<T>(T obj, bool update = false) {
				if(update) {
					_typeInstances.AddOrUpdate(typeof(T), obj, (t, o) => obj);
				} else {
					_typeInstances.TryAdd(typeof(T), obj);
				}

				return this;
			}

			public NServiceProvider Build()
				=> new NServiceProvider(_typeInstances);

			public ServiceProviderBuilder LoadFrom(Assembly assembly) {
				var allTypes = assembly.GetTypes();
				var services = new Queue<Type>(allTypes.Where(x => x.GetInterfaces().Contains(typeof(IMService)) && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract).ToArray());

				var interfaces = new HashSet<Type>(allTypes.Where(x => x.GetInterfaces().Contains(typeof(IMService)) && x.GetTypeInfo().IsInterface));

				var typeInstantiationFailures = new Dictionary<Type, int>();

				var sw         = Stopwatch.StartNew();
				var swInstance = new Stopwatch();
				while(services.Any()) {
					var type = services.Dequeue();

					if(_typeInstances.TryGetValue(type, out _)) continue;

					var constructor              = type.GetConstructors()[0];
					var constructorArgumentTypes = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

					var constructorArguments = constructorArgumentTypes.ToDictionary(argType => argType, argType => _typeInstances.TryGetValue(argType, out var argInstance) ? argInstance : null);

					if(constructorArguments.ContainsValue(null)) {
						services.Enqueue(type);

						if(typeInstantiationFailures.ContainsKey(type)) {
							typeInstantiationFailures[type]++;
							if(typeInstantiationFailures[type] > 3) {
								var missingArguments = constructorArguments.Where(kv => kv.Value == null).Select(kv => kv.Key.Name).ToArray();
								_log.Warn($"{type.Name} wasn't instantiated in the first 3 attempts. Missing type(s) {string.Join(",", missingArguments)}.");
							}
						} else
							typeInstantiationFailures.Add(type, 1);
					} else {
						swInstance.Restart();
						var instance = constructor.Invoke(constructorArguments.Values.ToArray());
						swInstance.Stop();

						if(swInstance.Elapsed.TotalSeconds > 5) _log.Info($"{type.Name} took {swInstance.Elapsed.TotalSeconds:F2}s to load.");

						var interfaceType = interfaces.FirstOrDefault(x => type.GetInterfaces().Contains(x));
						if(interfaceType != null) _typeInstances.TryAdd(interfaceType, instance);

						_typeInstances.TryAdd(type, instance);
					}
				}

				sw.Stop();
				_log.Info($"All services loaded in {sw.Elapsed.TotalSeconds:F2}s");

				return this;
			}

			public ServiceProviderBuilder FromServiceProvider(INServiceProvider provider) {
				foreach(var service in provider.Services) {
					_typeInstances.TryAdd(service.Key, service.Value);
				}
				
				return this;
			}
		}

		public ImmutableDictionary<Type, object> Services { get; }

		public NServiceProvider(IDictionary<Type, object> services) {
			Services = services.ToImmutableDictionary();
		}

		public T GetService<T>()
			=> (T)((IServiceProvider)this).GetService(typeof(T));

		object IServiceProvider.GetService(Type serviceType) {
			Services.TryGetValue(serviceType, out var toReturn);
			return toReturn;
		}

		IEnumerator IEnumerable.GetEnumerator()
			=> Services.Values.GetEnumerator();

		public IEnumerator<object> GetEnumerator()
			=> Services.Values.GetEnumerator();
	}
}