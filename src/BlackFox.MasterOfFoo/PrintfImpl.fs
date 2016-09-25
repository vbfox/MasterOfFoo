// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

module MasterOfFoo.Core.PrintfImpl

    open MasterOfFoo.Core.PrintfCache
   
    let inline doPrintf fmt f = 
        let formatter, n = Cache<_, _, _, _>.Get fmt
        let env() = f(n)
        formatter env
