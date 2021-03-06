﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2012-2016.

namespace Prime
open System.Collections
open System.Collections.Generic

[<AutoOpen>]
module UlistModule =

    type [<NoEquality; NoComparison>] 'a Ulist =
        private
            { RefList : 'a Tlist ref }
    
        interface 'a IEnumerable with
            member this.GetEnumerator () =
                let (seq, tlist) = Tlist.toSeq !this.RefList
                this.RefList := tlist
                seq.GetEnumerator ()
    
        interface IEnumerable with
            member this.GetEnumerator () =
                (this :> 'a IEnumerable).GetEnumerator () :> IEnumerator

        member this.Item index =
            let (result, tlist) = Tlist.get index !this.RefList
            this.RefList := tlist
            result

    [<RequireQualifiedAccess>]
    module Ulist =

        let makeFromSeq bloatFactorOpt items =
            { RefList = ref ^ Tlist.makeFromSeq bloatFactorOpt items }

        let makeEmpty<'a> bloatFactorOpt =
            { RefList = ref ^ Tlist.makeEmpty<'a> bloatFactorOpt }

        let singleton item =
            { RefList = ref ^ Tlist.singleton item }

        let get (index : int) (list : 'a Ulist) =
            list.[index]

        let set index value list =
            { RefList = ref ^ Tlist.set index value !list.RefList }

        let add value list =
            { RefList = ref ^ Tlist.add value !list.RefList }

        let remove value list =
            { RefList = ref ^ Tlist.remove value !list.RefList }

        let isEmpty list =
            let (result, tlist) = Tlist.isEmpty !list.RefList
            list.RefList := tlist
            result

        let notEmpty list =
            not ^ isEmpty list

        let contains value list =
            let (result, tlist) = Tlist.contains value !list.RefList
            list.RefList := tlist
            result

        let ofSeq values =
            { RefList = ref ^ Tlist.ofSeq values }

        let toSeq (list : _ Ulist) =
            list :> _ seq

        let fold folder state list =
            let (result, tlist) = Tlist.fold folder state !list.RefList
            list.RefList := tlist
            result

        let map mapper list =
            let (result, tlist) = Tlist.map mapper !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let filter pred list =
            let (result, tlist) = Tlist.filter pred !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let rev list =
            let (result, tlist) = Tlist.rev !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let sortWith comparison list =
            let (result, tlist) = Tlist.sortWith comparison !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let sortBy by list =
            let (result, tlist) = Tlist.sortBy by !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let sort list =
            let (result, tlist) = Tlist.sort !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let definitize list =
            let (result, tlist) = Tlist.definitize !list.RefList
            list.RefList := tlist
            { RefList = ref result }

        let concat lists =
            let tlists = !(map (fun (list : 'a Ulist) -> !list.RefList) lists).RefList
            let tlist = Tlist.concat tlists
            { RefList = ref tlist }

        /// Add all the given values to the list.
        let addMany values list =
            { RefList = ref ^ Tlist.addMany values !list.RefList }

        /// Remove all the given values from the list.
        let removeMany values list =
            { RefList = ref ^ Tlist.removeMany values !list.RefList }

type 'a Ulist = 'a UlistModule.Ulist