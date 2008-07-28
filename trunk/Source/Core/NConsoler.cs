﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace NConsoler
{
	/// <summary>
	/// Entry point for NConsoler applications
	/// </summary>
	public sealed class Consolery
	{
		/// <summary>
		/// Run an appropriate Action method.
		/// Uses the class this call lives in as target type and command line arguments from Environment
		/// </summary>
		public static void Run()
		{
			Type declaringType = new StackTrace().GetFrame(1).GetMethod().DeclaringType;
			string[] args = new string[Environment.GetCommandLineArgs().Length - 1];
			new List<string>(Environment.GetCommandLineArgs()).CopyTo(1, args, 0, Environment.GetCommandLineArgs().Length - 1);
			Run(declaringType, args);
		}

		/// <summary>
		/// Run an appropriate Action method
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		/// <param name="args">Arguments that will be converted to Action method arguments</param>
		public static void Run(Type targetType, string[] args)
		{
			Run(targetType, args, new ConsoleMessenger());
		}

		/// <summary>
		/// Run an appropriate Action method
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		/// <param name="args">Arguments that will be converted to Action method arguments</param>
		/// <param name="messenger">Uses for writing messages instead of Console class methods</param>
		public static void Run(Type targetType, string[] args, IMessenger messenger)
		{
			try
			{
				new Consolery(targetType, args, messenger).RunAction();
			}
			catch (NConsolerException e)
			{
				messenger.Write(e.Message);
			}
		}

		private readonly Type _targetType;
		private readonly string[] _args;
		private readonly List<MethodInfo> _actionMethods = new List<MethodInfo>();
		private readonly IMessenger _messenger;

		private Consolery(Type targetType, string[] args, IMessenger messenger)
		{
			#region Parameter Validation
			if (targetType == null)
			{
				throw new ArgumentNullException("targetType");
			}
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}
			if (messenger == null)
			{
				throw new ArgumentNullException("messenger");
			}
			#endregion
			_targetType = targetType;
			_args = args;
			_messenger = messenger;
			MethodInfo[] methods = _targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo method in methods)
			{
				object[] attributes = method.GetCustomAttributes(false);
				foreach (object attribute in attributes)
				{
					if (attribute is ActionAttribute)
					{
						_actionMethods.Add(method);
						break;
					}
				}
			}
		}

		static object ConvertValue(string value, Type argumentType)
		{
			if (argumentType == typeof(int))
			{
				try
				{
					return Convert.ToInt32(value);
				}
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to integer", value);
				}
				catch (OverflowException)
				{
					throw new NConsolerException("Value \"{0}\" is too big or too small", value);
				}
			}
			if (argumentType == typeof(string))
			{
				return value;
			}
			if (argumentType == typeof(bool))
			{
				try
				{
					return Convert.ToBoolean(value);
				}
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to boolean", value);
				}
			}
			if (argumentType == typeof(string[]))
			{
				return value.Split('+');
			}
			if (argumentType == typeof(int[]))
			{
				string[] values = value.Split('+');
				int[] valuesArray = new int[values.Length];
				for (int i = 0; i < values.Length; i++)
				{
					valuesArray[i] = (int)ConvertValue(values[i], typeof(int));
				}
				return valuesArray;
			}
			if (argumentType == typeof(DateTime))
			{
				string[] parts = value.Split('-');
				if (parts.Length != 3)
				{
					throw new NConsolerException("Could not convert {0} to Date", value);
				}
				int day = (int)ConvertValue(parts[0], typeof(int));
				int month = (int)ConvertValue(parts[1], typeof(int));
				int year = (int)ConvertValue(parts[2], typeof(int));
				try
				{
					return new DateTime(year, month, day);
				}
				catch (ArgumentException)
				{
					throw new NConsolerException("Could not convert {0} to Date", value);
				}
			}
			throw new NConsolerException("Unknown type is used in your method {0}", argumentType.FullName);
		}

		private void RunAction()
		{
			ValidateMetadata();
			if (IsHelpRequested())
			{
				PrintUsage();
				return;
			}

			MethodInfo currentMethod = GetCurrentMethod();

			ValidateInput(currentMethod);
			InvokeMethod(currentMethod);
		}

		private struct ParameterData
		{
			public readonly int position;
			public readonly Type type;

			public ParameterData(int position, Type type)
			{
				this.position = position;
				this.type = type;
			}
		}

		private static bool IsRequired(ICustomAttributeProvider info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(ParameterAttribute), false);
			return attributes.Length == 0 || attributes[0].GetType() == typeof(RequiredAttribute);
		}

		private static bool IsOptional(ICustomAttributeProvider info)
		{
			return !IsRequired(info);
		}

		private static OptionalAttribute GetOptional(ICustomAttributeProvider info)
		{
			object[] attributes = info.GetCustomAttributes(typeof(OptionalAttribute), false);
			return (OptionalAttribute)attributes[0];
		}

		private bool IsMulticommand
		{
			get
			{
				return _actionMethods.Count > 1;
			}

		}

		private bool IsHelpRequested()
		{
			return _args.Length == 0 || _args[0] == "/?" || _args[0] == "/help" || _args[0] == "/h";
		}

		private void InvokeMethod(MethodInfo method)
		{
			try
			{
				method.Invoke(null, BuildParameterArray(method));
			}
			catch (TargetInvocationException e)
			{
				if (e.InnerException != null)
				{
					throw e.InnerException;
				}
				throw;
			}
		}

		private object[] BuildParameterArray(MethodInfo method)
		{
			int argumentIndex = IsMulticommand ? 1 : 0;
			List<object> parameterValues = new List<object>();
			Dictionary<string, ParameterData> aliases = new Dictionary<string, ParameterData>();
			foreach (ParameterInfo info in method.GetParameters())
			{
				if (IsRequired(info))
				{
					parameterValues.Add(ConvertValue(_args[argumentIndex], info.ParameterType));
				}
				else
				{
					OptionalAttribute optional = GetOptional(info);

					foreach (string altName in optional.AltNames)
					{
						aliases.Add(altName,
							new ParameterData(parameterValues.Count, info.ParameterType));
					}
					aliases.Add(info.Name,
						new ParameterData(parameterValues.Count, info.ParameterType));
					parameterValues.Add(optional.Default);
				}
				argumentIndex++;
			}
			foreach (string optionalParameter in OptionalParameters(method))
			{
				string name = ParameterName(optionalParameter);
				string value = ParameterValue(optionalParameter);
				parameterValues[aliases[name].position] = ConvertValue(value, aliases[name].type);
			}
			return parameterValues.ToArray();
		}

		private IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			int firstOptionalParameterIndex = RequiredParameterCount(method);
			if (IsMulticommand)
			{
				firstOptionalParameterIndex++;
			}
			for (int i = firstOptionalParameterIndex; i < _args.Length; i++)
			{
				yield return _args[i];
			}
		}

		private static int RequiredParameterCount(MethodInfo method)
		{
			int requiredParameterCount = 0;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					requiredParameterCount++;
				}
			}
			return requiredParameterCount;
		}

		private MethodInfo GetCurrentMethod()
		{
			if (!IsMulticommand)
			{
				return _actionMethods[0];
			}
			string methodName = _args[0].ToLower();
			foreach (MethodInfo method in _actionMethods)
			{
				if (method.Name.ToLower() == methodName)
				{
					return method;
				}
			}
			PrintUsage();
			throw new NConsolerException("Unknown option \"{0}\"", _args[0]);
		}

		private void PrintUsage(MethodInfo method)
		{
			Dictionary<string, string> parameters = GetParametersDescriptions(method);
			PrintUsageExample(parameters);
			PrintParametersDescriptions(parameters);
		}

		private void PrintUsageExample(IDictionary<string, string> parameters)
		{
			_messenger.Write("usage: " + ProgramName() + " " + String.Join(" ", new List<string>(parameters.Keys).ToArray()));
		}

		private static Dictionary<string, string> GetParametersDescriptions(MethodInfo method)
		{
			Dictionary<string, string> parameters = new Dictionary<string, string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] parameterAttributes =
					parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
				if (parameterAttributes.Length > 0)
				{
					string name = GetDisplayName(parameter);
					ParameterAttribute attribute = (ParameterAttribute)parameterAttributes[0];
					parameters.Add(name, attribute.Description);
				}
				else
				{
					parameters.Add(parameter.Name, String.Empty);
				}
			}
			return parameters;
		}

		private void PrintParametersDescriptions(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			int maxParameterNameLength = MaxKeyLength(parameters);
			foreach (KeyValuePair<string, string> pair in parameters)
			{
				if (pair.Value != String.Empty)
				{
					int difference = maxParameterNameLength - pair.Key.Length + 2;
					_messenger.Write("    " + pair.Key + new String(' ', difference) + pair.Value);
				}
			}
		}

		private static int MaxKeyLength(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			int maxLength = 0;
			foreach (KeyValuePair<string, string> pair in parameters)
			{
				if (pair.Key.Length > maxLength)
				{
					maxLength = pair.Key.Length;
				}
			}
			return maxLength;
		}

		private string ProgramName()
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly == null)
			{
				return _targetType.Name.ToLower();
			}
			return new AssemblyName(entryAssembly.FullName).Name;
		}

		private void PrintUsage()
		{
			if (IsMulticommand)
			{
				_messenger.Write(
					String.Format("usage: {0} <subcommand> [args]", ProgramName()));
				_messenger.Write(
					String.Format("Type '{0} help <subcommand>' for help on a specific subcommand.", ProgramName()));
				_messenger.Write(String.Empty);
				_messenger.Write("Available subcommands:");
				foreach (MethodInfo method in _actionMethods)
				{
					_messenger.Write(method.Name.ToLower());
				}
			}
			else
			{
				PrintUsage(_actionMethods[0]);
			}
		}

		private static string GetDisplayName(ParameterInfo parameter)
		{
			if (IsRequired(parameter))
			{
				return parameter.Name;
			}
			OptionalAttribute optional = GetOptional(parameter);
			string parameterName =
				(optional.AltNames.Length > 0) ? optional.AltNames[0] : parameter.Name;
			if (parameter.ParameterType != typeof(bool))
			{
				parameterName += ":" + ValueDescription(parameter.ParameterType);
			}
			return "[/" + parameterName + "]";
		}

		private static string ValueDescription(Type type)
		{
			if (type == typeof(int))
			{
				return "number";
			}
			if (type == typeof(string))
			{
				return "value";
			}
			if (type == typeof(int[]))
			{
				return "number[+number]";
			}
			if (type == typeof(string[]))
			{
				return "value[+value]";
			}
			if (type == typeof(DateTime))
			{
				return "dd-mm-yyyy";
			}
			throw new ArgumentOutOfRangeException(String.Format("Type {0} is unknown", type.Name));
		}

		#region Validation

		private void ValidateInput(MethodInfo method)
		{
			CheckAllRequiredParametersAreSet(method);
			CheckOptionalParametersAreNotDuplicated(method);
			CheckUnknownParametersAreNotPassed(method);
		}

		private void CheckAllRequiredParametersAreSet(MethodInfo method)
		{
			int minimumArgsLengh = RequiredParameterCount(method);
			if (IsMulticommand)
			{
				minimumArgsLengh++;
			}
			if (_args.Length < minimumArgsLengh)
			{
				throw new NConsolerException("Not all required parameters are set");
			}
		}

		private static string ParameterName(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return parameter.Substring(2);
			}
			if (parameter.Contains(":"))
			{
				return parameter.Substring(1, parameter.IndexOf(":") - 1);
			}
			return parameter.Substring(1);
		}

		private static string ParameterValue(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return "false";
			}
			if (parameter.Contains(":"))
			{
				return parameter.Substring(parameter.IndexOf(":") + 1);
			}
			return "true";
		}

		private void CheckOptionalParametersAreNotDuplicated(MethodInfo method)
		{
			List<string> passedParameters = new List<string>();
			foreach (string optionalParameter in OptionalParameters(method))
			{
				if (!optionalParameter.StartsWith("/"))
				{
					throw new NConsolerException("Unknown parameter {0}", optionalParameter);
				}
				string name = ParameterName(optionalParameter);
				if (passedParameters.Contains(name))
				{
					throw new NConsolerException("Parameter with name {0} passed two times", name);
				}
				passedParameters.Add(name);
			}
		}

		private void CheckUnknownParametersAreNotPassed(MethodInfo method)
		{
			List<string> parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				parameterNames.Add(parameter.Name);
				OptionalAttribute optional = GetOptional(parameter);
				foreach (string altName in optional.AltNames)
				{
					parameterNames.Add(altName);
				}
			}
			foreach (string optionalParameter in OptionalParameters(method))
			{
				string name = ParameterName(optionalParameter);
				if (!parameterNames.Contains(name))
				{
					throw new NConsolerException("Unknown parameter name {0}", optionalParameter);
				}
			}
		}

		private void ValidateMetadata()
		{
			CheckAnyActionMethodExists();
			IfActionMethodIsSingleCheckMethodHasParameters();
			foreach (MethodInfo method in _actionMethods)
			{
				CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(method);
				CheckOptionalParametersAreAfterRequiredOnes(method);
				CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(method);
				CheckOptionalParametersAltNamesAreNotDuplicated(method);
			}
		}

		private void CheckAnyActionMethodExists()
		{
			if (_actionMethods.Count == 0)
			{
				throw new NConsolerException("Can not find any public static method marked with [Action] attribute in type \"{0}\"", _targetType.Name);
			}
		}

		private void IfActionMethodIsSingleCheckMethodHasParameters()
		{
			if (_actionMethods.Count == 1 && _actionMethods[0].GetParameters().Length == 0)
			{
				throw new NConsolerException("[Action] attribute applied once to the method \"{0}\" without parameters. In this case NConsoler should not be used", _actionMethods[0].Name);
			}
		}

		private static void CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] attributes = parameter.GetCustomAttributes(typeof(ParameterAttribute), false);
				if (attributes.Length > 1)
				{
					throw new NConsolerException("More than one attribute is applied to the parameter \"{0}\" in the method \"{1}\"", parameter.Name, method.Name);
				}
			}
		}

		private static void CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				OptionalAttribute optional = GetOptional(parameter);
				if (!optional.Default.GetType().IsAssignableFrom(parameter.ParameterType))
				{
					throw new NConsolerException("Default value for an optional parameter \"{0}\" in method \"{1}\" can not be assigned to the parameter", parameter.Name, method.Name);
				}
			}
		}

		private static void CheckOptionalParametersAreAfterRequiredOnes(MethodBase method)
		{
			bool optionalFound = false;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsOptional(parameter))
				{
					optionalFound = true;
				}
				else if (optionalFound)
				{
					throw new NConsolerException("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"", method.Name, parameter.Name);
				}
			}
		}

		private static void CheckOptionalParametersAltNamesAreNotDuplicated(MethodBase method)
		{
			List<string> parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					parameterNames.Add(parameter.Name);
				}
				else
				{
					if (parameterNames.Contains(parameter.Name))
					{
						throw new NConsolerException("Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters", parameter.Name, method.Name);
					}
					parameterNames.Add(parameter.Name);
					OptionalAttribute optional = GetOptional(parameter);
					foreach (string altName in optional.AltNames)
					{
						if (parameterNames.Contains(altName))
						{
							throw new NConsolerException("Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters", altName, method.Name);
						}
						parameterNames.Add(altName);
					}
				}
			}
		}

		#endregion
	}

	/// <summary>
	/// Used for getting messages from NConsoler
	/// </summary>
	public interface IMessenger
	{
		void Write(string message);
	}

	/// <summary>
	/// Uses Console class for message output
	/// </summary>
	public class ConsoleMessenger : IMessenger
	{
		public void Write(string message)
		{
			Console.WriteLine(message);
		}
	}

	/// <summary>
	/// Every action method should be marked with this attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class ActionAttribute : Attribute
	{
		public ActionAttribute()
		{
		}

		public ActionAttribute(string description)
		{
			_description = description;
		}

		private string _description = String.Empty;

		/// <summary>
		/// Description is used for help messages
		/// </summary>
		public string Description
		{
			get
			{
				return _description;
			}

			set
			{
				_description = value;
			}
		}
	}

	/// <summary>
	/// Should not be used directly
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class ParameterAttribute : Attribute
	{
		private string _description = String.Empty;

		/// <summary>
		/// Description is used in help message
		/// </summary>
		public string Description
		{
			get
			{
				return _description;
			}

			set
			{
				_description = value;
			}
		}

		protected ParameterAttribute()
		{
		}
	}

	/// <summary>
	/// Marks an Action method parameter as optional
	/// </summary>
	public sealed class OptionalAttribute : ParameterAttribute
	{
		private string[] _altNames;

		public string[] AltNames
		{
			get
			{
				return _altNames;
			}

			set
			{
				_altNames = value;
			}
		}

		private readonly object _defaultValue;

		public object Default
		{
			get
			{
				return _defaultValue;
			}
		}

		/// <param name="defaultValue">Default value if client doesn't pass this value</param>
		/// <param name="altNames">Aliases for parameter</param>
		public OptionalAttribute(object defaultValue, params string[] altNames)
		{
			_defaultValue = defaultValue;
			_altNames = altNames;
		}
	}

	/// <summary>
	/// Marks an Action method parameter as required
	/// </summary>
	public sealed class RequiredAttribute : ParameterAttribute
	{

	}

	/// <summary>
	/// Can be used for safe exception throwing - NConsoler will catch the exception
	/// </summary>
	public sealed class NConsolerException : Exception
	{
		public NConsolerException()
		{
		}

		public NConsolerException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public NConsolerException(string message, params string[] arguments)
			: base(String.Format(message, arguments))
		{
		}
	}
}