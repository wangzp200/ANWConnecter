using System;
using System.Reflection;

namespace AnwConnector.Util.httpHelper.CsharpHttpHelper.Helper
{
	internal class ExecJsHelper
	{
		private static Type _type = Type.GetTypeFromProgID("ScriptControl");

		internal static string JavaScriptEval(string strJs, string main)
		{
			object scriptControl = ExecJsHelper.GetScriptControl();
			ExecJsHelper.SetScriptControlType(strJs, scriptControl);
			return ExecJsHelper._type.InvokeMember("Eval", BindingFlags.InvokeMethod, null, scriptControl, new object[]
			{
				main
			}).ToString();
		}

		private static Type SetScriptControlType(string strJs, object obj)
		{
			ExecJsHelper._type.InvokeMember("Language", BindingFlags.SetProperty, null, obj, new object[]
			{
				"JScript"
			});
			ExecJsHelper._type.InvokeMember("AddCode", BindingFlags.InvokeMethod, null, obj, new object[]
			{
				strJs
			});
			return ExecJsHelper._type;
		}

		private static object GetScriptControl()
		{
			return Activator.CreateInstance(ExecJsHelper._type);
		}
	}
}
