﻿using System;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using NUnit.Framework;
using Rhino.Mocks;

namespace NConsoler.Tests
{
	[TestFixture]
	public class SimpleScenarios
	{
		private static dynamic verifier;

		[SetUp]
		public void Setup()
		{
			verifier = new ExpandoObject();
		}

		[Test]
		public void OneParameter()
		{
			Consolery.Run(typeof(OneParameterProgram), new[] { "parameter" });

			Assert.That(verifier.parameter, Is.EqualTo("parameter"));
		}

		public class OneParameterProgram
		{
			[Action]
			public static void RunProgram([Required]string parameter)
			{
				verifier.parameter = parameter;
			}
		}

		[Test]
		public void Should_run_program_when_only_optional_parameters_specified()
		{
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new string[] { });

			Assert.That(verifier.parameter, Is.EqualTo(true));
		}

		[Test]
		public void Should_run_program_when_specified_case_for_optional_argument_inconsistent_with_actual_parameters()
		{
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new[] { "/-PARAMETER" });

			Assert.That(verifier.parameter, Is.EqualTo(false));
		}

		public class OnlyOptionalParametersProgram
		{
			[Action]
			public static void RunProgram([Optional(true)]bool parameter)
			{
				verifier.parameter = parameter;
			}
		}

		[Test]
		public void ManyParameters()
		{
			Consolery.Run(typeof(ManyParametersProgram),
				new[] { "string", "1", "true", "/os:string", "/oi:1", "/ob" });

			Assert.That(verifier.sParameter, Is.EqualTo("string"));
			Assert.That(verifier.iParameter, Is.EqualTo(1));
			Assert.That(verifier.bParameter, Is.EqualTo(true));
			Assert.That(verifier.osParameter, Is.EqualTo("string"));
			Assert.That(verifier.oiParameter, Is.EqualTo(1));
			Assert.That(verifier.obParameter, Is.EqualTo(true));
		}

		[Test]
		public void RunConsoleProgramWithoutOptionalParameters()
		{
			Consolery.Run(typeof(ManyParametersProgram),
				new[] { "string", "1", "true" });

			Assert.That(verifier.sParameter, Is.EqualTo("string"));
			Assert.That(verifier.iParameter, Is.EqualTo(1));
			Assert.That(verifier.bParameter, Is.EqualTo(true));
			Assert.That(verifier.osParameter, Is.EqualTo("0"));
			Assert.That(verifier.oiParameter, Is.EqualTo(0));
			Assert.That(verifier.obParameter, Is.EqualTo(false));
		}

		[Test]
		public void NegativeBooleanParameter()
		{
			Consolery.Run(typeof(ManyParametersProgram),
				new[] { "string", "1", "true", "/-ob" });

			Assert.That(verifier.sParameter, Is.EqualTo("string"));
			Assert.That(verifier.iParameter, Is.EqualTo(1));
			Assert.That(verifier.bParameter, Is.EqualTo(true));
			Assert.That(verifier.osParameter, Is.EqualTo("0"));
			Assert.That(verifier.oiParameter, Is.EqualTo(0));
			Assert.That(verifier.obParameter, Is.EqualTo(false));
		}

		public class ManyParametersProgram
		{
			[Action]
			public static void RunProgram(
				[Required]
				string sParameter,
				int iParameter,
				[Required]
				bool bParameter,
				[Optional("0", "os")]
				string osParameter,
				[Optional(0, "oi")]
				int oiParameter,
				[Optional(false, "ob")]
				bool obParameter)
			{
				verifier.sParameter = sParameter;
				verifier.iParameter = iParameter;
				verifier.bParameter = bParameter;
				verifier.osParameter = osParameter;
				verifier.oiParameter = oiParameter;
				verifier.obParameter = obParameter;
			}
		}

		[Test]
		public void NullableParameter()
		{
			Consolery.Run(typeof(NullableParameterProgram), new[] { "10" });

			Assert.That(verifier.i, Is.EqualTo(10));
		}

		public class NullableParameterProgram
		{
			[Action]
			public static void RunProgram([Required]int? i)
			{
				verifier.i = i;
			}
		}

		[Test]
		public void EnumParameterTest()
		{
			Consolery.Run(typeof(EnumParameterProgram), new[] { "One" });

			Assert.That(verifier.testEnum, Is.EqualTo(TestEnum.One));
		}

		public class EnumParameterProgram
		{
			[Action]
			public static void RunProgram([Required]TestEnum testEnum)
			{
				verifier.testEnum = testEnum;
			}
		}

		public enum TestEnum
		{
			One,
			Two
		}

		[Test]
		public void EnumDecimalTest()
		{
			Consolery.Run(typeof(EnumDecimalProgram), new[] { "1" });

			Assert.That(verifier.d, Is.EqualTo(1));
		}

		public class EnumDecimalProgram
		{
			[Action]
			public static void RunProgram([Required]decimal d)
			{
				verifier.d = d;
			}
		}

		[Test]
		public void should_work_with_instance_actions()
		{
			var instance = new InstanceActionsProgram();
			Consolery.Run(instance, new[] { "test" });

			Assert.That(verifier.arg, Is.EqualTo("test"));
		}

		public class InstanceActionsProgram
		{
			[Action]
			public void Test(string arg)
			{
				verifier.arg = arg;
			}
		}

		[Test]
		public void Should_work_with_net40_optional_arguments()
		{
			Consolery.Run(typeof(Net40OptionalArgumentsProgram), new[] { "1" });

			Assert.That(verifier.required, Is.EqualTo(1));
			Assert.That(verifier.optional, Is.EqualTo(true));
		}

		public class Net40OptionalArgumentsProgram
		{
			[Action]
			public static void Test(int required, bool optional = true)
			{
				verifier.required = required;
				verifier.optional = optional;
			}
		}

		[Test]
		public void Should_work_without_arguments_in_action()
		{
			Consolery.Run(typeof(WithoutArgumentsProgram), new[] { "Test" });

			Assert.That(verifier.TestCalled, Is.True);
		}

		public class WithoutArgumentsProgram
		{
			[Action]
			public static void Test()
			{
				verifier.TestCalled = true;
			}

			[Action]
			public static void Test1()
			{
				verifier.Test1Called = true;
			}
		}

		[Test]
		public void when_specified_case_for_optional_argument_inconsistent_with_actual_parameters()
		{
			Consolery.Run(typeof(OnlyOptionalParametersProgram), new[] { "/-PARAMETER" });

			Assert.That(verifier.parameter, Is.EqualTo(false));
		}

		public class ExceptionalProgram
	{
		public static int RunCount;

		[Action]
		public static void RunProgram([Optional(true)]bool parameter)
		{
			throw new SpecificException();
		}
	}

	public class SpecificException : Exception
	{
		public SpecificException() {}
	}

		[Test]
		public void when_metadata_validation_fails_should_set_error_code()
		{
			Consolery.Run(typeof(ExceptionalProgram), new[] { "wrong" });

			Assert.That(Environment.ExitCode, Is.EqualTo(1));
		}

		[Test]
		public void when_target_method_throws_an_exception()
		{
			Exception exception = null;
			try
			{
				Consolery.Run(typeof (ExceptionalProgram), new[] {"/-parameter"});
			}
			catch(Exception e)
			{
				exception = e;
			}

			Assert.That(exception, Is.Not.Null);
			Assert.That(exception.GetType(), Is.EqualTo(typeof(SpecificException)));
		}
	}

	[TestFixture]
	public class NConsolerTest
	{
		MockRepository mocks;
		static IMessenger messenger;

		[SetUp]
		public void Setup()
		{
			mocks = new MockRepository();
			messenger = mocks.CreateMock<IMessenger>();
		}

		public class OneParameterProgram
		{
			[Action]
			public static void RunProgram([Required]string parameter)
			{
				messenger.Write(parameter);
			}
		}

		public class ManyParametersProgram
		{
			[Action]
			public static void RunProgram(
				[Required]
				string sParameter,
				int iParameter,
				[Required]
				bool bParameter,
				[Optional("0", "os")]
				string osParameter,
				[Optional(0, "oi")]
				int oiParameter,
				[Optional(false, "ob")]
				bool obParameter)
			{
				messenger.Write(
					String.Format("{0} {1} {2} {3} {4} {5}", 
						sParameter, iParameter, bParameter, osParameter, oiParameter, obParameter));
			}
		}

		[Test]
		public void WithoutMethods()
		{
			messenger.Write("Can not find any public static method marked with [Action] attribute in type \"WithoutMethodsProgram\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(WithoutMethodsProgram), new[] { "string" }, messenger);
		}

		public class WithoutMethodsProgram
		{
		}

		[Test]
		public void WrongParameterOrder()
		{
			messenger.Write("It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"RunProgram\" parameter \"requiredParameter\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(WrongParameterOrderProgram), 
				new[] { "string" }, messenger);
		}

		public class WrongParameterOrderProgram
		{
			[Action]
			public static void RunProgram(
				[Optional("0")]string optionalParameter,
				[Required]string requiredParameter)
			{

			}
		}

		[Test]
		public void WrongDefaultValueForOptionalStringParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(WrongDefaultValueForOptionalStringParameterProgram),
				new string[] { }, messenger);
		}

		public class WrongDefaultValueForOptionalStringParameterProgram
		{
			[Action]
			public static void RunProgram(
				[Optional(10)]string optionalParameter)
			{

			}
		}

		[Test]
		public void WrongDefaultValueForOptionalIntegerParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(WrongDefaultValueForOptionalIntegerParameterProgram),
				new string[] { }, messenger);
		}

		public class WrongDefaultValueForOptionalIntegerParameterProgram
		{
			[Action]
			public static void RunProgram(
				[Optional("test")]int optionalParameter)
			{

			}
		}

		[Test]
		public void VeryBigDefaultValueForOptionalIntegerParameter()
		{
			messenger.Write("Default value for an optional parameter \"optionalParameter\" in method \"RunProgram\" can not be assigned to the parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(VeryBigDefaultValueForOptionalIntegerParameterProgram),
				new string[] { }, messenger);
		}

		public class VeryBigDefaultValueForOptionalIntegerParameterProgram
		{
			[Action]
			public static void RunProgram(
				[Optional("1234567890")]int optionalParameter)
			{

			}
		}

		[Test]
		public void DuplicatedParameterNames()
		{
			messenger.Write("Found duplicated parameter name \"a\" in method \"RunProgram\". Please check alt names for optional parameters");
			mocks.ReplayAll();
			Consolery.Run(typeof(DuplicatedParameterNamesProgram),
				new string[] { }, messenger);
		}

		public class DuplicatedParameterNamesProgram
		{
			[Action]
			public static void RunProgram(
				[Optional(1, "a")]int optionalParameter1,
				[Optional(2, "a")]int optionalParameter2)
			{

			}
		}

		[Test]
		public void DuplicatedParameterAttributes()
		{
			messenger.Write("More than one attribute is applied to the parameter \"parameter\" in the method \"RunProgram\"");
			mocks.ReplayAll();
			Consolery.Run(typeof(DuplicatedParameterAttributesProgram),
				new[] { "parameter" }, messenger);
		}

		public class DuplicatedParameterAttributesProgram
		{
			[Action]
			public static void RunProgram(
				[Required][Optional("")]string parameter)
			{
			}
		}

		[Test]
		public void NotAllRequiredParametersAreSet()
		{
			messenger.Write("usage: manyparametersprogram sParameter iParameter bParameter [/os:value] [/oi:number] [/ob]");
			messenger.Write("    [/os:value]");
			messenger.Write("        default value: '0'");
			messenger.Write("    [/oi:number]");
			messenger.Write("        default value: 0");
			messenger.Write("    [/ob]");
			messenger.Write("        default value: False");
			messenger.Write("Error: Not all required parameters are set");
			mocks.ReplayAll();
			Consolery.Run(typeof(ManyParametersProgram),
				new[] { "test" }, messenger);
		}

		[Test]
		public void UnknownParameter()
		{
			messenger.Write("Unknown parameter name /unknown:value");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram),
				new[] { "required", "/unknown:value" }, messenger);
		}

		[Test]
		public void UnknownBooleanParameterWithNegativeSign()
		{
			messenger.Write("Unknown parameter name /-unknown");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram),
				new[] { "required", "/-unknown" }, messenger);
		}

		[Test]
		public void UnknownBooleanParameter()
		{
			messenger.Write("Unknown parameter name /unknown");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneParameterProgram),
				new[] { "required", "/unknown" }, messenger);
		}

		[Test]
		public void Should_choose_correct_method_if_more_than_one_method()
		{
			messenger.Write("m2test");
			mocks.ReplayAll();
			Consolery.Run(typeof(TwoActionsProgram),
				new[] { "Test2", "test" }, messenger);
		}

		[Test]
		public void Should_show_help_for_a_particular_message()
		{
			messenger.Write("usage: twoactionsprogram test2 parameter");
			mocks.ReplayAll();
			Consolery.Run(typeof(TwoActionsProgram), new[] { "help", "Test2" }, messenger);
		}

		public class TwoActionsProgram
		{
			[Action]
			public static void Test1(
				[Required]string parameter)
			{
				messenger.Write("m1" + parameter);
			}

			[Action]
			public static void Test2(
				[Required]string parameter)
			{
				messenger.Write("m2" + parameter);
			}
		}

		[Test]
		public void Should_show_default_value_for_optional_parameter()
		{
			messenger.Write("usage: oneactionprogramwithoptionalparameters [/parameter1:value] [/param2:number]");
			messenger.Write("    [/parameter1:value]  param1 desc");
			messenger.Write("        default value: 'value1'");
			messenger.Write("    [/param2:number]     desc2");
			messenger.Write("        default value: 42");
			mocks.ReplayAll();
			Consolery.Run(typeof(OneActionProgramWithOptionalParameters), new[] { "help", "Test" }, messenger);
		}

		public class OneActionProgramWithOptionalParameters
		{
			[Action]
			public static void Test(
				[Optional("value1", Description = "param1 desc")]string parameter1,
				[Optional(42, Description = "desc2")]int param2)
			{
				messenger.Write("m1" + param2);
			}
		}

		[Test]
		public void Should_work_without_arguments_in_action()
		{
			messenger.Write("test");
			mocks.ReplayAll();
			Consolery.Run(typeof(WithoutArgumentsProgram),
				new[] { "Test" }, messenger);
		}

		public class WithoutArgumentsProgram
		{
			[Action]
			public static void Test()
			{
				messenger.Write("test");
			}

			[Action]
			public static void Test1()
			{
				messenger.Write("test1");
			}
		}

		public class OptionalDateTimeProgram
		{
			[Action]
			public static void Test(DateTime required, [Optional("31-12-2008", "dtDate")]DateTime optional)
			{
				messenger.Write(optional.ToString("dd-MM-yyyy"));
			}
		}

		[Test]
		public void Should_correctly_convert_to_datetime_from_optional_attribute_default_value()
		{
			messenger.Write("31-12-2008");
			mocks.ReplayAll();
			Consolery.Run(typeof(OptionalDateTimeProgram), new[] { "01-01-2009", "/dtDate:31-12-2008" }, messenger);
		}

		[TearDown]
		public void Teardown()
		{
			mocks.VerifyAll();
		}
	}
}