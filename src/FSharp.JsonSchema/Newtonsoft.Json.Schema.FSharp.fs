﻿namespace Newtonsoft.Json.Schema.Generation

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Newtonsoft.Json.Linq
open Newtonsoft.Json.FSharp.Idiomatic
open Newtonsoft.Json.Serialization
open Newtonsoft.Json.Schema
open Newtonsoft.Json.Schema.Generation

type OptionGenerationProvider() =
    inherit JSchemaGenerationProvider()

    static let optionTy = typedefof<option<_>>

    override __.CanGenerateSchema(context:JSchemaTypeGenerationContext) =
        context.ObjectType.IsGenericType
        && optionTy.Equals(context.ObjectType.GetGenericTypeDefinition())

    override __.GetSchema(context:JSchemaTypeGenerationContext) =
        let cases = FSharpType.GetUnionCases(context.ObjectType)
        let schema = JSchema(Type=Nullable JSchemaType.Object)
        for case in cases do
            match case.Name with
            | "None" ->
                schema.AnyOf.Add(JSchema(Type=Nullable JSchemaType.Null))
            | _ ->
                let field = case.GetFields() |> Array.head
                let propSchema = context.Generator.Generate(field.PropertyType)
                schema.AnyOf.Add(propSchema)
        schema

type SingleCaseDuGenerationProvider() =
    inherit JSchemaGenerationProvider()

    override __.CanGenerateSchema(context:JSchemaTypeGenerationContext) =
        FSharpType.IsUnion(context.ObjectType)
        && Reflection.allCasesEmpty context.ObjectType

    override __.GetSchema(context:JSchemaTypeGenerationContext) =
        let cases = FSharpType.GetUnionCases(context.ObjectType)
        let schema = JSchema(Type=Nullable JSchemaType.String)
        for case in cases do
            schema.Enum.Add(JValue case.Name)
        schema

type MultiCaseDuGenerationProvider(?casePropertyName) =
    inherit JSchemaGenerationProvider()

    let casePropertyName = defaultArg casePropertyName "kind"

    override __.CanGenerateSchema(context:JSchemaTypeGenerationContext) =
        FSharpType.IsUnion(context.ObjectType)
        && not (Reflection.allCasesEmpty context.ObjectType)
        && not (Reflection.isList context.ObjectType)
        && not (Reflection.isOption context.ObjectType)

    override __.GetSchema(context:JSchemaTypeGenerationContext) =
        let cases = FSharpType.GetUnionCases(context.ObjectType)
        let schema = JSchema(Type=Nullable JSchemaType.Object)
        for case in cases do
            let propSchema = JSchema(Type=Nullable JSchemaType.Object)
            propSchema.Properties.Add(casePropertyName, JSchema(Type=Nullable JSchemaType.String))
            let fields = case.GetFields()
            for field in fields do
                let fieldSchema = context.Generator.Generate(field.PropertyType)
                propSchema.Properties.Add(KeyValuePair(field.Name, fieldSchema))
            schema.AnyOf.Add(propSchema)
        schema

[<AutoOpen>]
module Extensions =

    type Newtonsoft.Json.Schema.Generation.JSchemaGenerator with

        static member Create(?casePropertyName) =
            let generator = JSchemaGenerator(ContractResolver=CamelCasePropertyNamesContractResolver())

            generator.GenerationProviders.Add(StringEnumGenerationProvider())
            generator.GenerationProviders.Add(OptionGenerationProvider())
            generator.GenerationProviders.Add(SingleCaseDuGenerationProvider())
            generator.GenerationProviders.Add(MultiCaseDuGenerationProvider(?casePropertyName=casePropertyName))

            generator