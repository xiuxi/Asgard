// <copyright file="ConfigTest.cs" company="enBask Studios">Copyright ©  2015</copyright>
using System;
using Asgard.Core.Utils;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Asgard.Utils.Tests
{
    /// <summary>This class contains parameterized unit tests for Config</summary>
    [PexClass(typeof(Config))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class ConfigTest
    {
        /// <summary>Test stub for Load(String)</summary>
        [PexMethod]
        public void LoadTest(string config)
        {
            Config.Load(config);
            // TODO: add assertions to method ConfigTest.LoadTest(String)
        }

        /// <summary>Test stub for GetString(String)</summary>
        [PexMethod]
        public string GetStringTest(string key)
        {
            string result = Config.GetString(key);
            return result;
            // TODO: add assertions to method ConfigTest.GetStringTest(String)
        }

        /// <summary>Test stub for GetInt(String)</summary>
        [PexMethod]
        public int GetIntTest(string key)
        {
            int result = Config.GetInt(key);
            return result;
            // TODO: add assertions to method ConfigTest.GetIntTest(String)
        }

        /// <summary>Test stub for Get(String)</summary>
        [PexGenericArguments(typeof(object))]
        [PexMethod]
        public T GetTest<T>(string key)
        {
            T result = Config.Get<T>(key);
            return result;
            // TODO: add assertions to method ConfigTest.GetTest(String)
        }
    }
}
