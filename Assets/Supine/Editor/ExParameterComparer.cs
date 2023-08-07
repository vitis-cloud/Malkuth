using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using ExpressionParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;

class ExParameterComparer : IEqualityComparer<ExpressionParameter>
{
    public bool Equals(ExpressionParameter x, ExpressionParameter y)
    {
        return x.name == y.name && x.valueType == y.valueType;
    }

    public int GetHashCode(ExpressionParameter parameter)
    {
        return ( parameter.name + parameter.valueType.ToString()).GetHashCode();
    }
}