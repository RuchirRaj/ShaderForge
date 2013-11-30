using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ShaderForge {

	// Used to detect types based on input
	[System.Serializable]
	public class SFNCG_Arithmetic : SF_NodeConnectionGroup {


		public bool lockedOutput = false;

		public SFNCG_Arithmetic() {

		}

		public SFNCG_Arithmetic Initialize( SF_NodeConnection output, params SF_NodeConnection[] inputs ) {
			this.output = output;
			this.inputs = inputs;
			return this;
		}

		public SFNCG_Arithmetic LockOutType() {
			lockedOutput = true;
			return this;
		}


		public override void Refresh() {
			// Are none of the inputs connected? In that case, it's all default
			if( NoInputsConnected() )
				ResetValueTypes();

			// If any input is non-pending, use that as base to assign the rest.
			// Inputs:
			ValueType baseInType = GetBaseInputType();
			ValueType genericInType = GetGenericInputType();
			AssignToEmptyInputs( genericInType );

			//Debug.Log("Refreshing connection group of " + output.node.nodeName );

			// Output:
			if( !lockedOutput ){
				if( InputsMissing() ) {
					if( baseInType == ValueType.VTv1 )
						output.valueType = ValueType.VTvPending;
					else
						output.valueType = baseInType;
				} else {
				//	Debug.Log("SEARCHING:");
					ValueType vtDom = GetDominantInputType();
					//Debug.Log("Dominant = " + vtDom);
					SetOutputValueType(vtDom);

					UpdateTypecasts();
				}
			}
		}


		public void SetOutputValueType(ValueType vt){

			//Debug.Log("Trying to set to " + vt);
			output.SetValueType(vt);

			//Debug.Log("Setting output type of " + output.node.nodeName + " to " + output.valueType); // THIS IS SET TO PENDING VOR VEC1 INPUTS
		}
		
		
		// This is only run if there are no inputs missing!
		public void UpdateTypecasts(){
			ValueType domType = output.valueType;
			
			
			
			// Reset typecasts
			foreach(SF_NodeConnection con in inputs)
				con.typecastTarget = 0;
			
			if(domType == ValueType.VTv1 || domType == ValueType.VTv1v2 || domType == ValueType.VTv2)
				return; // No typecasting
			
			int typeTarget = 0;
			// If the dominant type is Vector3, cast all Vector2 to v3
			if(domType == ValueType.VTv1v3 || domType == ValueType.VTv3){
				typeTarget = 3;
			} else if(domType == ValueType.VTv1v4 || domType == ValueType.VTv4){
				typeTarget = 4;
			} else {
				//Debug.LogError("Shouldn't be able to get here, invalid casting on "+base.output.node.GetType().ToString() + " domType = " + domType.ToString());
			}
			
			
			foreach(SF_NodeConnection con in inputs){
				if(con.GetCompCount() == 2)
					con.TypecastTo(typeTarget);
			}
			
		}


		public ValueType GetGenericInputType() {
			ValueType vt = GetBaseInputType();
			Debug.Log("Getting base input type on "+output.node.nodeName+" = " + vt);
			switch( vt ) {
				case ValueType.VTv1:
					if(inputs.Length > 1)
						return ValueType.VTvPending; // TODO: Really?
					else
						return ValueType.VTv1; // TODO: This feels weird
				case ValueType.VTv2:
					return ValueType.VTv1v2;
				case ValueType.VTv3:
					return ValueType.VTv1v3;
				case ValueType.VTv4:
					return ValueType.VTv1v4;
				default:
					//Debug.LogWarning( "Invalid attempt to get generic input type from " + vt );
					return ValueType.VTvPending;
			}
		}

		public ValueType GetDominantInputType() {

			ValueType dom = inputs[0].valueType;


			//ValueType dom = inputs[0].valueType;
			//Debug.Log("Val 0 is " + inputs[0].valueType.ToString());
			//Debug.Log("Val 1 is " + inputs[1].valueType.ToString());


			for( int i = 1; i < inputs.Length; i++ ) {
				dom = GetDominantType( dom, inputs[i].valueType );
			}
			//Debug.Log("Found dominant type:" + dom.ToString());
			return dom;
		}

		public ValueType GetDominantType( ValueType a, ValueType b ) {
			
			//Debug.Log("Checking dominancy between a:" + a.ToString() + " b:" + b.ToString() + " on " + output.node.nodeName);
			
			if( a == b )
				return a;

			if( a == ValueType.VTvPending )
				return b;

			if( b == ValueType.VTvPending )
				return a;

			if( a == ValueType.VTv1 ) {
				if( IsVectorType( b ) )
					return b;
				else
					return a;
			}
			if( b == ValueType.VTv1 ) {
				if( IsVectorType( a ) )
					return a;
				else
					return b;
			}
			
			if(a == ValueType.VTv2 || a == ValueType.VTv1v2)
				return b;
			if(b == ValueType.VTv2 || b == ValueType.VTv1v2)
				return a;
			
			
			if(a == ValueType.VTv3 && b == ValueType.VTv4)
				return b;
			if(b == ValueType.VTv3 && a == ValueType.VTv4)
				return a;
			
			
			

		//	Debug.LogWarning( "You should not be able to get here! Dominant pending type returned" );
			return ValueType.VTvPending;
		}


		public ValueType GetBaseInputType() {

			ValueType retType = ValueType.VTvPending;

			foreach( SF_NodeConnection nc in inputs ) {
				retType = GetDominantType( retType, nc.valueType );
			}

			//if( retType == ValueType.VTvPending )
				//Debug.LogWarning( "You should not be able to get here! Pending type returned" );
			return retType;
		}

		public static bool CompatibleTypes( ValueType tInput, ValueType tOutput ) {

			// If they are the same type, they are of course compatible
			if( tInput == tOutput )
				return true;
			// If the input is a pending vector, any output vector is compatible
			if( tInput == ValueType.VTvPending && IsVectorType( tOutput ) )
				return true;
			// Check multi-type for v1/v2
			if( tInput == ValueType.VTv1v2 && ( tOutput == ValueType.VTv1 || tOutput == ValueType.VTv2 ) )
				return true;
			// Check multi-type for v1/v3
			if( tInput == ValueType.VTv1v3 && ( tOutput == ValueType.VTv1 || tOutput == ValueType.VTv3 ) )
				return true;
			// Check multi-type for v1/v4
			if( tInput == ValueType.VTv1v4 && ( tOutput == ValueType.VTv1 || tOutput == ValueType.VTv4 ) )
				return true;
			// Didn't find any allowed link, return false
			return false;
		}

	}
}