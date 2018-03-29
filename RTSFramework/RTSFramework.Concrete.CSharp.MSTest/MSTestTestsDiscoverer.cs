﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using RTSFramework.Concrete.CSharp.MSTest.Models;
using RTSFramework.Contracts;

namespace RTSFramework.Concrete.CSharp.MSTest
{
    public class MSTestTestsDiscoverer : ITestsDiscoverer<MSTestTestcase>
    {
        public IEnumerable<string> Sources { private get; set; }

        //TODO: Think about discovery of tests based on source code (using ASTs)
        //Advantage: can identify impacted tests even if code does not compile
        //Disadvantage: maybe requires lot more time?
        public IEnumerable<MSTestTestcase> GetTestCases()
        {
            var testCases = new List<MSTestTestcase>();

            foreach (var modulePath in Sources)
            {
                ModuleDefinition module = GetMonoModuleDefinition(modulePath);
                foreach (TypeDefinition type in module.Types)
                {
                    if (type.HasMethods)
                    {
                        foreach (MethodDefinition method in type.Methods)
                        {
                            if (method.CustomAttributes.Any(x => x.AttributeType.Name == MSTestConstants.TestMethodAttributeName))
                            {
                                var testCase = new MSTestTestcase(modulePath, method.Name, type.FullName);

                                var categoryAttributes =
                                    method.CustomAttributes.Where(x => x.AttributeType.Name == MSTestConstants.TestCategoryAttributeName);
                                foreach (var categoryAttr in categoryAttributes)
                                {
                                    testCase.Categories.Add((string)categoryAttr.ConstructorArguments[0].Value);
                                }

                                testCase.Ignored = method.CustomAttributes.Any(x => x.AttributeType.Name == MSTestConstants.IgnoreAttributeName);

                                testCases.Add(testCase);
                            }
                        }
                    }
                }
            }

            return testCases;
        }

        private ModuleDefinition GetMonoModuleDefinition(string moduleFilePath)
        {
            FileInfo fileInfo = new FileInfo(moduleFilePath);
            if (!fileInfo.Exists)
            {
                throw new ArgumentException("Invalid path to a module: " + moduleFilePath);
            }
            ModuleDefinition module;

            ReaderParameters parameters = new ReaderParameters { ReadSymbols = true };
            try
            {
                module = ModuleDefinition.ReadModule(moduleFilePath, parameters);
            }
            catch (Exception)
            {
                module = ModuleDefinition.ReadModule(moduleFilePath);
            }

            return module;
        }

        
    }
}