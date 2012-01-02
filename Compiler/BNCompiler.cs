using System;
using System.Collections.Generic;
using BasicN.Tokenizer;
using System.Reflection;
using System.Reflection.Emit;
using BasicN.Lib;

namespace BasicN.Compiler {
	internal class CompilerContext {
		public class OutputInterface {
			public Type Type = typeof( IContext );
			public MethodInfo Print = typeof( IContext ).GetMethod( "Print", new[] { typeof( string ) } );
			public MethodInfo PrintLine = typeof( IContext ).GetMethod( "PrintLine", new[] { typeof( string ) } );
			public MethodInfo Cls = typeof( IContext ).GetMethod( "Cls" );
			public MethodInfo Read = typeof( IContext ).GetMethod( "Read" );
			public MethodInfo ReadLine = typeof( IContext ).GetMethod( "ReadLine" );
			public MethodInfo Locate = typeof( IContext ).GetMethod( "Locate", new[] { typeof( int ), typeof( int ) } );
		}

		public AssemblyBuilder Assembly;
		public ModuleBuilder Module;
		public TypeBuilder MainType;
		public string FileName;
		public string MainTypeName;

		public OutputInterface DefaultOutput = new OutputInterface();
		public Type DefaultOutputClass = typeof( DefaultConsoleContext );
		public TypeBuilder ProgramType;
		public Type Program;
		public MethodBuilder ProgramMain;
		public ILGenerator ProgramIL;
		public FieldBuilder ProgramDataList;

	}

	public partial class NBCompiler {
		public static bool Compile(IErrorPrinter errorPrinter, IEnumerable<string> program, string outputType, string fileName) {
			TokenizerOutput to;
			try {
				to = NBTokenizer.Tokenize( errorPrinter, program );
			}
			catch( TokenizerException te ) {
				Console.WriteLine( "Compilation error!" );
				Console.WriteLine( te.Line.OriginalLine.OriginalLine );
				Console.WriteLine( te.Message );
				to = null;
			}

			if( to == null )
				return false;

			CompilerContext context = CreateCompilerContext( outputType, fileName );
			CreateProgramType( context, to );
			CreateMainTypeAndSave( context );
			
			return true;
		}

		public static bool Compile(IErrorPrinter errorPrinter, string sourcePath, string outputType, string fileName) {
			return Compile( errorPrinter, NBTokenizer.GetLines( sourcePath ), outputType, fileName );
		}

		// helpers
		static Type[] argString = new Type[] { typeof( string ) };
		static Type[] argInt = new Type[] { typeof( int ) };
		static Type retString = typeof( string );
		static Type retVoid = typeof( void );
	}
}
