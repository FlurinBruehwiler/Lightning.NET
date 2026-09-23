/* browser-wasm build of LMDB: mdb.c, midl.c, lmdb.h and midl.h are copied from the MDB_MEMORY branch of
 * https://github.com/FlurinBruehwiler/lmdb (LMDB 0.9.35 plus MDB_MEMORY) at commit ddaa876.
 *
 * The file name is load-bearing: the .NET wasm build registers one pinvoke module per NativeFileReference, named
 * after the file, and it has to match DllImport("lmdb").
 *
 * The defines live here because NativeFileReference sources are compiled during the emcc link step, which does
 * not receive EmccExtraCFlags. Emscripten's libc has no robust mutexes; nothing locks anyway, since browser
 * environments are opened with MDB_MEMORY (which implies MDB_NOLOCK).
 */
#define MDB_USE_POSIX_MUTEX 1
#define MDB_USE_ROBUST 0

#include "mdb.c"
#include "midl.c"
