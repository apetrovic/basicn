// BasicN, copyright (c) Aleksandar Petrovic, 2008 - 2011
// (see accompanying copyright.txt)

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using BasicN.Tokenizer;

namespace BasicN.Compiler {
	public partial class NBCompiler {
		private static CompilerContext CreateCompilerContext(string outputType, string fileName) {
			var context = new CompilerContext();

			AssemblyName assemblyName = new AssemblyName { Name = outputType };
			context.Assembly = Thread.GetDomain().DefineDynamicAssembly( assemblyName, AssemblyBuilderAccess.Save );
			context.Module = context.Assembly.DefineDynamicModule( fileName, fileName );

			context.FileName = fileName;
			context.MainTypeName = outputType;

			return context;
		}

		private static void CreateMainTypeAndSave(CompilerContext context) {
			context.MainType = context.Module.DefineType( context.MainTypeName );
			MethodBuilder method = context.MainType.DefineMethod( "Main", MethodAttributes.Static | MethodAttributes.Public, retVoid, null );
			method.InitLocals = true;
			ILGenerator il = method.GetILGenerator();
			Label jmp = il.DefineLabel();
			il.MarkLabel( jmp );
			il.Emit( OpCodes.Newobj, context.Program.GetConstructor( Type.EmptyTypes ) );
			il.Emit( OpCodes.Newobj, context.DefaultOutputClass.GetConstructor( Type.EmptyTypes ) );
			il.Emit( OpCodes.Call, context.ProgramMain );
			il.Emit( OpCodes.Brfalse, jmp );
			il.Emit( OpCodes.Ret );

			context.Assembly.SetEntryPoint( method );
			context.MainType.CreateType();
			context.Assembly.Save( context.FileName );
		}

		private static void CreateProgramType(CompilerContext context, TokenizerOutput to) {
			TypeBuilder type = context.Module.DefineType( context.MainTypeName + "Program", TypeAttributes.Class | TypeAttributes.Public );
			type.DefineDefaultConstructor( MethodAttributes.Public );

			context.ProgramType = type;
			FillDataArray( context, to );

			MethodBuilder main = type.DefineMethod( "Main", MethodAttributes.Public, typeof( bool ), new[] {  context.DefaultOutput.Type } );
			ILGenerator il = main.GetILGenerator();

			context.ProgramMain = main;
			context.ProgramIL = il;


			DoCompile( context, to );

			context.Program = type.CreateType();
		}

		private static void FillDataArray(CompilerContext context, TokenizerOutput to) {
			ConstructorBuilder ctor = context.ProgramType.DefineConstructor( MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes );
			FieldBuilder array = context.ProgramType.DefineField( "DataArray", typeof( string[] ), FieldAttributes.Static | FieldAttributes.Public );
			context.ProgramDataList = array;
			ILGenerator il = ctor.GetILGenerator();

			if( to.Data != null && to.Data.Count > 0 ) {

				il.Emit( OpCodes.Ldc_I4, to.Data.Count );
				il.Emit( OpCodes.Newarr, typeof( string ) );
				il.Emit( OpCodes.Stsfld, array );

				for( int i = 0; i < to.Data.Count; ++i ) {
					il.Emit( OpCodes.Ldsfld, array );
					il.Emit( OpCodes.Ldc_I4, i );
					il.Emit( OpCodes.Ldstr, to.Data[i] );
					il.Emit( OpCodes.Stelem_Ref );
				}
			}

			il.Emit( OpCodes.Ret );
		}
	}
}
