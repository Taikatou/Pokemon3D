﻿using Pokemon3D.Scripting.Types;
using Pokemon3D.Scripting.Types.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pokemon3D.Scripting.Adapters
{
    /// <summary>
    /// An adapter to convert .Net objects to script objects.
    /// </summary>
    public static class ScriptInAdapter
    {
        /// <summary>
        /// Returns the "undefined" script object.
        /// </summary>
        public static SObject GetUndefined(ScriptProcessor processor)
        {
            return processor.Undefined;
        }

        /// <summary>
        /// Translates a .Net object to a script object.
        /// </summary>
        public static SObject Translate(ScriptProcessor processor, object objIn)
        {
            // todo: C# 7: put a swtich statement type match instead of aweful if case blocks.
            if (objIn == null)
            {
                return TranslateNull(processor);
            }

            if (objIn.GetType() == typeof(SObject) || objIn.GetType().IsSubclassOf(typeof(SObject)))
            {
                // this is already an SObject, return it.
                return (SObject)objIn;
            }

            if (objIn is sbyte || objIn is byte || objIn is short || objIn is ushort || objIn is int || objIn is uint || objIn is long || objIn is ulong || objIn is float || objIn is double)
            {
                return TranslateNumber(processor, Convert.ToDouble(objIn));
            }
            else if (objIn is string || objIn is char)
            {
                return TranslateString(processor, objIn.ToString()); // ToString will return just the string for the string type, and a string from a char.
            }
            else if (objIn is bool)
            {
                return TranslateBool(processor, (bool)objIn);
            }
            else if (objIn is Type)
            {
                return TranslatePrototype(processor, (Type)objIn);
            }
            else if (objIn.GetType().IsArray)
            {
                return TranslateArray(processor, (Array)objIn);
            }
            else if (objIn is BuiltInMethod || objIn is DotNetBuiltInMethod)
            {
                return TranslateFunction((Delegate)objIn);
            }
            else if (objIn is ScriptRuntimeException)
            {
                return TranslateException((ScriptRuntimeException)objIn);
            }
            else
            {
                return TranslateObject(processor, objIn);
            }
        }

        private static SObject TranslateNull(ScriptProcessor processor)
        {
            return processor.Null;
        }

        private static SObject TranslateNumber(ScriptProcessor processor, double dblIn)
        {
            return processor.CreateNumber(dblIn);
        }

        private static SObject TranslateString(ScriptProcessor processor, string strIn)
        {
            return processor.CreateString(strIn);
        }

        private static SObject TranslateBool(ScriptProcessor processor, bool boolIn)
        {
            return processor.CreateBool(boolIn);
        }

        private static SObject TranslateFunction(Delegate methodIn)
        {
            return new SFunction(methodIn);
        }

        private static SObject TranslateArray(ScriptProcessor processor, Array array)
        {
            List<SObject> elements = new List<SObject>();

            for (int i = 0; i < array.Length; i++)
                elements.Add(Translate(processor, array.GetValue(i)));

            return processor.Context.CreateInstance("Array", elements.ToArray());
        }

        private static SObject TranslateException(ScriptRuntimeException exceptionIn)
        {
            return exceptionIn.ErrorObject;
        }

        private static SObject TranslateObject(ScriptProcessor processor, object objIn)
        {
            var objType = objIn.GetType();

            string typeName = objType.Name;
            ScriptPrototypeAttribute customNameAttribute = objType.GetCustomAttribute<ScriptPrototypeAttribute>();
            if (customNameAttribute != null && !string.IsNullOrWhiteSpace(customNameAttribute.VariableName))
                typeName = customNameAttribute.VariableName;

            Prototype prototype = null;

            if (processor.Context.IsPrototype(typeName))
                prototype = processor.Context.GetPrototype(typeName);
            else
                prototype = TranslatePrototype(processor, objIn.GetType());

            var obj = prototype.CreateInstance(processor, null, false);

            // Set the field values of the current instance:

            FieldInfo[] fields = objIn.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                .ToArray();

            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(false);

                foreach (var attr in attributes)
                {
                    if (attr.GetType() == typeof(ScriptVariableAttribute))
                    {
                        var memberAttr = (ScriptMemberAttribute)attr;

                        string identifier = field.Name;
                        if (!string.IsNullOrEmpty(memberAttr.VariableName))
                            identifier = memberAttr.VariableName;

                        var fieldContent = field.GetValue(objIn);

                        obj.SetMember(identifier, Translate(processor, fieldContent));
                    }
                    else if (attr.GetType() == typeof(ScriptFunctionAttribute))
                    {
                        // When it's a field and a function, we have the source code of the function as value of the field.
                        // Example: public string MyFunction = "function() { console.log('Hello World'); }";

                        var memberAttr = (ScriptMemberAttribute)attr;

                        string identifier = field.Name;
                        if (!string.IsNullOrEmpty(memberAttr.VariableName))
                            identifier = memberAttr.VariableName;

                        string functionCode = field.GetValue(objIn).ToString();

                        obj.SetMember(identifier, new SFunction(processor, functionCode));
                    }
                }
            }

            return obj;
        }

        internal static Prototype TranslatePrototype(ScriptProcessor processor, Type t)
        {
            string name = t.Name;
            ScriptPrototypeAttribute customNameAttribute = t.GetCustomAttribute<ScriptPrototypeAttribute>();
            if (customNameAttribute != null && !string.IsNullOrWhiteSpace(customNameAttribute.VariableName))
                name = customNameAttribute.VariableName;

            var prototype = new Prototype(name);

            object typeInstance = null;
            if (!t.IsAbstract)
            {
                typeInstance = Activator.CreateInstance(t);
            }

            FieldInfo[] fields = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                .ToArray();

            foreach (var field in fields)
            {
                var attributes = field.GetCustomAttributes(false);

                foreach (var attr in attributes)
                {
                    if (attr.GetType() == typeof(ScriptVariableAttribute))
                    {
                        var memberAttr = (ScriptMemberAttribute)attr;
                        string identifier = field.Name;
                        if (!string.IsNullOrEmpty(memberAttr.VariableName))
                            identifier = memberAttr.VariableName;

                        var fieldContent = field.GetValue(typeInstance);

                        if (fieldContent == null)
                            prototype.AddMember(processor, new PrototypeMember(identifier, processor.Undefined, field.IsStatic, field.IsInitOnly, false, false));
                        else
                            prototype.AddMember(processor, new PrototypeMember(identifier, Translate(processor, fieldContent), field.IsStatic, field.IsInitOnly, false, false));
                    }
                    else if (attr.GetType() == typeof(ScriptFunctionAttribute))
                    {
                        var memberAttr = (ScriptFunctionAttribute)attr;
                        string identifier = field.Name;
                        if (!string.IsNullOrEmpty(memberAttr.VariableName))
                            identifier = memberAttr.VariableName;

                        var fieldContent = field.GetValue(typeInstance);

                        if (memberAttr.IndexerGet && memberAttr.IndexerSet)
                            throw new InvalidOperationException("The member function " + field.Name + " was marked both as an indexer set and indexer get. It can only be one at a time.");

                        if (fieldContent == null)
                        {
                            if (memberAttr.IndexerGet || memberAttr.IndexerSet)
                                throw new InvalidOperationException("A member function marked with Indexer Set or Indexer Get has to be defined.");

                            prototype.AddMember(processor, new PrototypeMember(identifier, processor.Undefined, field.IsStatic, field.IsInitOnly, false, false));
                        }
                        else
                        {
                            if (memberAttr.IndexerGet)
                                prototype.IndexerGetFunction = new SFunction(processor, fieldContent.ToString());
                            else if (memberAttr.IndexerSet)
                                prototype.IndexerSetFunction = new SFunction(processor, fieldContent.ToString());
                            else
                                prototype.AddMember(processor, new PrototypeMember(identifier, new SFunction(processor, fieldContent.ToString()), field.IsStatic, field.IsInitOnly, false, false));
                        }
                    }
                }
            }

            MethodInfo[] methods = t
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                .ToArray();

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ScriptFunctionAttribute>(false);
                if (attr != null)
                {
                    string identifier = method.Name;
                    if (!string.IsNullOrEmpty(attr.VariableName))
                        identifier = attr.VariableName;

                    if (attr.IndexerGet && attr.IndexerSet)
                        throw new InvalidOperationException("The member function " + method.Name + " was marked both as an indexer set and indexer get. It can only be one at a time.");

                    Delegate methodDelegate = null;

                    if (method.GetParameters().Length == 2)
                    {
                        // two parameter means the method is a DotNetBuiltInMethod.
                        methodDelegate = (DotNetBuiltInMethod)Delegate.CreateDelegate(typeof(DotNetBuiltInMethod), method);
                    }
                    else if (method.GetParameters().Length == 4)
                    {
                        // four parameters means that the method is a valid BuiltInMethod.
                        methodDelegate = (BuiltInMethod)Delegate.CreateDelegate(typeof(BuiltInMethod), method);
                    }

                    if (attr.IndexerGet)
                        prototype.IndexerGetFunction = new SFunction(methodDelegate);
                    else if (attr.IndexerSet)
                        prototype.IndexerSetFunction = new SFunction(methodDelegate);
                    else
                        prototype.AddMember(processor, new PrototypeMember(identifier, new SFunction(methodDelegate), method.IsStatic, true, false, false));
                }
            }

            prototype.MappedType = t;

            return prototype;
        }
    }
}
