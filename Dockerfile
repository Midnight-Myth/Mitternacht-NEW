FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /source
COPY . .
RUN dotnet publish -c Release -o /build

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS runtime
RUN apk add tzdata nano
COPY --from=build /build /build
WORKDIR /data
EXPOSE 5000
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_URLS="http://*:5000"
CMD ["dotnet", "/build/MitternachtWeb.dll"]
