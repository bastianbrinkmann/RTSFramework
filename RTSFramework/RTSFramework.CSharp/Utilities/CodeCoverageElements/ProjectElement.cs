//
// visual-coverage: ProjectElement.cs
//
// Author:
//   Joaquin Sargiotto (joaquinsargiotto@gmail.com)
// Contributor(s):
//
//
// Copyright (c) 2012 Joaquin Sargiotto
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using RTSFramework.Concrete.CSharp.Utilities.CodeCoverageMetrics;

namespace RTSFramework.Concrete.CSharp.Utilities.CodeCoverageElements
{
    public class ProjectElement
    {
        private string _name = "";
        private uint _timestamp = 0;
        private List<PackageElement> _elements = new List<PackageElement>();
    
        public ProjectElement ( string name, uint timestamp ) {
            _name = name;
            _timestamp = timestamp;
        }
        
        public String Name
        {
            get { return _name; }
            set { _name = value; }
        }
        
        public uint Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }
        
        public ProjectMetrics Metrics
        {
            get {
                // File metrics are calculated based on the classes
                // inside this file.
                ProjectMetrics pm = new ProjectMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (uint)_elements.Count);
                foreach ( PackageElement pe in GetPackages())
                {
                    pm.Add(pe.Metrics);
                }
                // Complexity is fixed, we shouldn't add this metric.
                pm.SetMetric("complexity", 1);
                return pm;
            }
        }
        
        public void AddPackage ( PackageElement e )
        {
            _elements.Add(e);
        }
        
        public List<PackageElement> GetPackages ()
        {
            return _elements;
        }
    }
}