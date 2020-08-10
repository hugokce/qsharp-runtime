// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.TargetGeneration

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.CodeAnalysis.Formatting

open Microsoft.Quantum.RoslynWrapper

open System
open System.Collections.Generic
open System.IO
open Microsoft.CodeAnalysis
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CsharpGeneration
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations
open System.Collections.Immutable

module Utils =
    let TargetQualifiedClassKey = "TargetClass"
    let ExtendsTargetKey = "TargetToExtend"

    let namespaceElementIsIntrinsic elem =
        match elem with
        | QsCallable callable -> 
            if callable.Specializations |> Seq.exists (fun spec -> match spec.Implementation with
                                                                   | Intrinsic -> true
                                                                   | _ ->         false)
            then Some callable
            else None
        | _ -> None

    let findIntrinsics syntaxTree =
        syntaxTree 
        |> Seq.collect (fun ns -> ns.Elements |> Seq.choose namespaceElementIsIntrinsic)

open Utils
open GenerateTarget

type TargetGenerationContext =
    {
        assemblyConstants   : IDictionary<string, string>
        intrinsics          : IEnumerable<QsCallable>
    }
    with

    static member Create(syntaxTree, assemblyConstants) =
        { assemblyConstants = assemblyConstants; intrinsics = syntaxTree |> findIntrinsics }

    static member Create(syntaxTree) =
        { assemblyConstants = ImmutableDictionary.Empty; intrinsics = syntaxTree |> findIntrinsics }

    member this.GenerateTarget(dir) =
        let fullName = this.assemblyConstants.[TargetQualifiedClassKey]
        let targetNamespace = fullName.Substring(0, fullName.LastIndexOf('.'))
        let targetClassName = fullName.Substring(fullName.LastIndexOf('.') + 1)
        let baseClassName = match this.assemblyConstants.TryGetValue ExtendsTargetKey with | (true, s) -> Some s | (false, _) -> None
        let outputFileName = Path.Combine(dir, targetClassName + ".cs")

        ()