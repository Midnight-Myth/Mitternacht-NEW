using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MitternachtWeb.Helpers {
	[HtmlTargetElement("input", Attributes = ForAttributeName)]
	public class DisabledInputTagHelper : InputTagHelper {
		private const string ForAttributeName = "asp-for";

		[HtmlAttributeName("asp-disabled")]
		public bool Disabled { set; get; }

		public DisabledInputTagHelper(IHtmlGenerator generator) : base(generator) { }

		public override void Process(TagHelperContext context, TagHelperOutput output) {
			if(Disabled) {
				var d = new TagHelperAttribute("disabled");
				output.Attributes.Add(d);
			}
			base.Process(context, output);
		}
	}
}
