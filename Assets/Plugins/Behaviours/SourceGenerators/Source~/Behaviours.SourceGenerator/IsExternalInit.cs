using System.ComponentModel;

//
// HACK: To make records work on netstandard2.0 we can define the missing type 'IsExternalInit' ourselves.
//
namespace System.Runtime.CompilerServices {
	[EditorBrowsable(EditorBrowsableState.Never)]
	public record IsExternalInit;
}
