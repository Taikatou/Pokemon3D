﻿namespace Pokemon3D.Scripting.Types
{
    /// <summary>
    /// Represents the "undefined" literal as object.
    /// </summary>
    class SUndefined : SObject
    {
        private SUndefined() { }
        
        /// <summary>
        /// Creates the undefined object.
        /// </summary>
        internal static SUndefined Factory()
        {
            return new SUndefined();
        }

        internal override SBool ToBool(ScriptProcessor processor)
        {
            return processor.CreateBool(false);
        }

        internal override string TypeOf()
        {
            return LITERAL_UNDEFINED;
        }
    }
}
