using System;
using ChildProject;

namespace ParentProject
{
    public class Parent
    {
        public Parent(SimpleChild child)
        {
            var _ = new ComplexChild();
        }
    }
}
