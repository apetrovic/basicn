// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using System.Collections.Generic;
using BasicN.Parser;
using System.Reflection.Emit;
using System.Reflection;

namespace BasicN.Compiler {
	internal class Null { }

	internal interface CItem {
		Type Type { get; }
		Type Execute(CompilingContext c, Statement statement);
	}

	internal abstract class BaseOp<T> : CItem {
		public Type Type { get { return typeof( T ); } }
		public abstract Type Execute(CompilingContext c, Statement statement);
	}

	internal class VoidOp<T> : BaseOp<T> {
		private readonly Action<CompilingContext, T> _action;
		public VoidOp(Action<CompilingContext, T> action) {
			_action = action;
		}

		public override Type Execute(CompilingContext c, Statement statement) {
			T s = (T)statement;
			_action( c, s );
			return typeof( Null );
		}
	}

	internal class TypedOp<T, RetType> : VoidOp<T> {
		public TypedOp(Action<CompilingContext, T> action) : base( action ) { }

		public override Type Execute(CompilingContext c, Statement statement) {
			base.Execute( c, statement );
			return typeof( RetType );
		}
	}

	internal interface CVariable : CItem {
		void Prepare(CompilingContext c, Statement statement);
		void SetValue(CompilingContext c, Statement statement);
		Type VariableType { get; }
	}

	internal class Variable<T, VarType> : BaseOp<T>, CVariable {
		private static FieldBuilder GetField(CompilingContext c, Statement statement) {
			Variable v = (Variable)statement;
			return c.GetVariable( v.Name, typeof( VarType ) );
		}

		public override Type Execute(CompilingContext c, Statement statement) {
			Prepare( c, statement );
			c.IL.Emit( OpCodes.Ldfld, GetField( c, statement ) );
			return typeof( VarType );
		}

		public void Prepare(CompilingContext c, Statement statement) {
			c.IL.Emit( OpCodes.Ldarg_0 );
		}

		public void SetValue(CompilingContext c, Statement statement) {
			c.IL.Emit( OpCodes.Stfld, GetField( c, statement ) );
		}

		public Type VariableType { get { return typeof( VarType ); } }
	}


	internal class VariableArray<T, VarType> : BaseOp<T>, CVariable {
		private static FieldBuilder GetField(CompilingContext c, Statement statement) {
			Variable v = (Variable)statement;
			return c.GetArray( v.Name );
		}

		public void Prepare(CompilingContext c, Statement statement) {
			c.IL.Emit( OpCodes.Ldarg_0 );
			c.IL.Emit( OpCodes.Ldfld, GetField( c, statement ) );

			VariableArray v = (VariableArray)statement;

			foreach( NumStatement n in v.Dimensions ) {
				c.Factory.Execute( c, n );
				c.IL.Emit( OpCodes.Conv_I4 );
			}
		}

		public override Type Execute(CompilingContext c, Statement statement) {
			Prepare( c, statement );

			VariableArray arr = (VariableArray)statement;
			var callTypes = new Type[arr.Dimensions.Count];
			for( int i = 0; i < callTypes.Length; ++i )
				callTypes[i] = typeof( int );

			c.IL.Emit( OpCodes.Call, GetField( c, statement ).FieldType.GetMethod( "Get", callTypes ) );
			return typeof( VarType );
		}

		public void SetValue(CompilingContext c, Statement statement) {
			Type fieldType = GetField( c, statement ).FieldType;
			VariableArray arr = (VariableArray)statement;
			var callTypes = new Type[arr.Dimensions.Count + 1];
			for( int i = 0; i < callTypes.Length - 1; ++i )
				callTypes[i] = typeof( int );

			callTypes[callTypes.Length - 1] = typeof( VarType );
			MethodInfo set = fieldType.GetMethod( "Set", callTypes );
			c.IL.Emit( OpCodes.Call, set );
		}

		public Type VariableType { get { return typeof( VarType ); } }
	}

	internal class Factory {
		private readonly Dictionary<string, CItem> _factory = new Dictionary<string, CItem>();

		internal void Add<T>(Action<CompilingContext, T> action) {
			var op = new VoidOp<T>( action );
			_factory.Add( op.Type.Name, op );
		}

		internal void Add<T, RetType>(Action<CompilingContext, T> action) {
			var op = new TypedOp<T, RetType>( action );
			_factory.Add( op.Type.Name, op );
		}

		internal void AddVariable<T, RetType>() {
			var v = new Variable<T, RetType>();
			_factory.Add( v.Type.Name, v );
		}

		internal void AddArray<T, RetType>() {
			var v = new VariableArray<T, RetType>();
			_factory.Add( v.Type.Name, v );
		}

		public CItem GetItem(string type) {
			return _factory[type];
		}

		public CItem GetItem(Type type) {
			return _factory[type.Name];
		}

		public CItem Make(string typeName) {
			CItem ret = GetItem( typeName );

			if( ret == null )
				throw new Exception( "Unknown type " + typeName + "!!" );

			return ret;
		}

		public Type Execute(CompilingContext c, Statement s) {
			CItem ret = Make( s.GetType().Name );
			return ret.Execute( c, s );
		}
	}
}
