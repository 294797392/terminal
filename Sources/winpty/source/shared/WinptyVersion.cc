// Copyright (c) 2015 Ryan Prichard
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

#include "WinptyVersion.h"

#include <stdio.h>
#include <string.h>

#include "DebugClient.h"

// This header is auto-generated by either the Makefile (Unix) or
// UpdateGenVersion.bat (gyp).  It is placed in a 'gen' directory, which is
// added to the search path.
//#include "GenVersion.h"

void dumpVersionToStdout() {
    //printf("winpty version %s\n", GenVersion_Version);
    //printf("commit %s\n", GenVersion_Commit);
}

void dumpVersionToTrace() {
    //trace("winpty version %s (commit %s)",
    //    GenVersion_Version,
    //    GenVersion_Commit);
}
