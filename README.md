**Santol** is an CIL-to-LLVM compiler. It takes (highly) compliant CIL code and replaces the stack based operations with SSA form equivalents allowing for LLVM IR to be generated. The outputted code is VM free, meaning it is AOT compiled to use a select few helper functions at runtime (for managing memory and exceptions, like the C++ runtime).

### Features
-	Most mathematical operations
-	Most conditional operations 
-	Branching
-	Static calls
-	Static members (methods, fields, constants)
-	Enums
-	Structs (Sequential only)
-	Direct memory access (pointers)
-	Primitive type conversions

### Todo
-	Exceptions
-	Objects (+ boxing)
-	Arrays
-	Inheritance (Interfaces + Virtual calls)
-	Advanced structs (non-sequential + interfaces)
-	Memory management (Garbage collection)

The ‘mscorlib’ is not implemented yet, meaning most inbuilt classes do not exist. Therefore, any code that requires them will not compile. A few classes are partly hard coded directly in (such as the base representation of ints and objects) however most methods in these classes are not supported. It is not known at this point whether a custom corlib will be wrote from scratch or whether the mscorlib will be ran through Santol with the native method calls patched. 

The produced LLVM code does not aim to follow the CIL standard. Types on the stack are not always expanded to int32s and are reduced wherever possible. However, the input CIL code must strictly follow the CIL standard, especially when it comes to state of the stack. Most importantly, the stack must be empty on a backwards branch if no forward branch exists to that location. Without these assumptions the algorithms would become a lot more complex leading to less efficient output, however if support for non-compliant code is needed, a fallback mode can be added.
