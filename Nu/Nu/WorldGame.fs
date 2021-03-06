﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2012-2016.

namespace Nu
open System
open System.IO
open Prime
open Nu

[<AutoOpen>]
module WorldGameModule =

    type Game with

        member this.GetId world = World.getGameId world
        member this.Id = PropertyTag.makeReadOnly this Property? Id this.GetId
        member this.GetXtension world = World.getGameXtension world
        member this.Xtension = PropertyTag.makeReadOnly this Property? Xtension this.GetXtension
        member this.GetDispatcherNp world = World.getGameDispatcherNp world
        member this.DispatcherNp = PropertyTag.makeReadOnly this Property? DispatcherNp this.GetDispatcherNp
        member this.GetSpecialization world = World.getGameSpecialization world
        member this.Specialization = PropertyTag.makeReadOnly this Property? Specialization this.GetSpecialization
        member this.GetClassification world = Classification.make (getTypeName ^ this.GetDispatcherNp world) (this.GetSpecialization world)
        member this.Classification = PropertyTag.makeReadOnly this Property? Classification this.GetClassification
        member this.GetCreationTimeStampNp world = World.getGameCreationTimeStampNp world
        member this.CreationTimeStampNp = PropertyTag.makeReadOnly this Property? CreationTimeStampNp this.GetCreationTimeStampNp
        member this.GetSelectedScreenOpt world = World.getSelectedScreenOpt world
        member this.SetSelectedScreenOpt value world = World.setSelectedScreenOpt value world
        member this.SelectedScreenOpt = PropertyTag.makeReadOnly this Property? SelectedScreenOpt this.GetSelectedScreenOpt
        member this.GetScreenTransitionDestinationOpt world = World.getScreenTransitionDestinationOpt world
        member this.SetScreenTransitionDestinationOpt value world = World.setScreenTransitionDestinationOpt value world
        member this.ScreenTransitionDestinationOpt = PropertyTag.makeReadOnly this Property? ScreenTransitionDestinationOpt this.GetScreenTransitionDestinationOpt
        member this.GetEyeCenter world = World.getEyeCenter world
        member this.SetEyeCenter value world = World.setEyeCenter value world
        member this.EyeCenter = PropertyTag.makeReadOnly this Property? EyeCenter this.GetEyeCenter
        member this.GetEyeSize world = World.getEyeSize world
        member this.SetEyeSize value world = World.setEyeSize value world
        member this.EyeSize = PropertyTag.makeReadOnly this Property? EyeSize this.GetEyeSize

        /// Get a property value and type.
        member this.GetProperty propertyName world = World.getGameProperty propertyName world

        /// Set a property value with explicit type.
        member this.SetProperty propertyName property world = World.setGameProperty propertyName property world

        /// Get a property value.
        member this.Get<'a> propertyName world : 'a = World.getGameProperty propertyName world |> fst :?> 'a

        /// Set a property value.
        member this.Set<'a> propertyName (value : 'a) world = World.setGameProperty propertyName (value :> obj, typeof<'a>) world

        /// Get the view of the eye in absolute terms (world space).
        member this.GetViewAbsolute (_ : World) = World.getViewAbsolute
        
        /// Get the view of the eye in absolute terms (world space) with translation sliced on
        /// integers.
        member this.GetViewAbsoluteI (_ : World) = World.getViewAbsoluteI

        /// The relative view of the eye with original single values. Due to the problems with
        /// SDL_RenderCopyEx as described in Math.fs, using this function to decide on sprite
        /// coordinates is very, very bad for rendering.
        member this.GetViewRelative world = World.getViewRelative world

        /// The relative view of the eye with translation sliced on integers. Good for rendering.
        member this.GetViewRelativeI world = World.getViewRelativeI world

        /// Get the bounds of the eye's sight relative to its position.
        member this.GetViewBoundsRelative world = World.getViewBoundsRelative world

        /// Get the bounds of the eye's sight not relative to its position.
        member this.GetViewBoundsAbsolute world = World.getViewAbsolute world

        /// Get the bounds of the eye's sight.
        member this.GetViewBounds viewType world = World.getViewBounds viewType world

        /// Check that the given bounds is within the eye's sight.
        member this.InView viewType bounds world = World.inView viewType bounds world

        /// Transform the given mouse position to screen space.
        member this.MouseToScreen mousePosition world = World.mouseToScreen mousePosition world

        /// Transform the given mouse position to world space.
        member this.MouseToWorld viewType mousePosition world = World.mouseToWorld viewType mousePosition world

        /// Transform the given mouse position to entity space.
        member this.MouseToEntity viewType entityPosition mousePosition world = World.mouseToEntity viewType entityPosition mousePosition world

        /// Check that a group dispatches in the same manner as the dispatcher with the target type.
        member this.DispatchesAs (dispatcherTargetType : Type) world = Reflection.dispatchesAs dispatcherTargetType (this.GetDispatcherNp world)

    type World with

        static member internal registerGame (world : World) : World =
            let dispatcher = Simulants.Game.GetDispatcherNp world
            let world = World.withEventContext (fun world -> dispatcher.Register (Simulants.Game, world)) (atooa Simulants.Game.GameAddress) world
            World.choose world
        
        static member internal updateGame world =
            World.withEventContext (fun world ->
                let dispatcher = Simulants.Game.GetDispatcherNp world
                let world = dispatcher.Update (Simulants.Game, world)
                let eventTrace = EventTrace.record "World" "updateGame" EventTrace.empty
                let world = World.publish7 World.sortSubscriptionsByHierarchy () Events.Update eventTrace Simulants.Game true world
                World.choose world)
                (atooa Simulants.Game.GameAddress)
                world

        static member internal postUpdateGame world =
            World.withEventContext (fun world ->
                let dispatcher = Simulants.Game.GetDispatcherNp world
                let world = dispatcher.PostUpdate (Simulants.Game, world)
                let eventTrace = EventTrace.record "World" "postUpdateGame" EventTrace.empty
                let world = World.publish7 World.sortSubscriptionsByHierarchy () Events.PostUpdate eventTrace Simulants.Game true world
                World.choose world)
                (atooa Simulants.Game.GameAddress)
                world

        static member internal actualizeGame world =
            World.withEventContext (fun world ->
                let dispatcher = Simulants.Game.GetDispatcherNp world
                let world = dispatcher.Actualize (Simulants.Game, world)
                World.choose world)
                (atooa Simulants.Game.GameAddress)
                world

        // Get all the entities in the world.
        static member getEntities1 world =
            World.getGroups1 world |>
            Seq.map (fun group -> World.getEntities group world) |>
            Seq.concat

        // Get all the groups in the world.
        static member getGroups1 world =
            World.getScreens world |>
            Seq.map (fun screen -> World.getGroups screen world) |>
            Seq.concat

        /// Determine if an entity is selected by being in a group of the currently selected screeen.
        static member isEntitySelected entity world =
            let screenName = Address.head entity.EntityAddress
            match World.getSelectedScreenOpt world with
            | Some selectedScreen -> screenName = Address.getName selectedScreen.ScreenAddress
            | None -> false

        /// Determine if a group is selected by being in the currently selected screeen.
        static member isGroupSelected group world =
            let screenName = Address.head group.GroupAddress
            match World.getSelectedScreenOpt world with
            | Some selectedScreen -> screenName = Address.getName selectedScreen.ScreenAddress
            | None -> false

        /// Determine if a screen is the currently selected screeen.
        static member isScreenSelected screen world =
            World.getSelectedScreenOpt world = Some screen

        /// Determine if a simulant is contained by, or is the same as, the currently selected screen.
        /// Game is always considered 'selected' as well.
        static member isSimulantSelected (simulant : Simulant) world =
            match Address.getNames simulant.SimulantAddress with
            | [] -> true
            | screenName :: _ ->
                match World.getSelectedScreenOpt world with
                | Some screen -> Address.getName screen.ScreenAddress = screenName
                | None -> false

        /// Write a game to a game descriptor.
        static member writeGame gameDescriptor world =
            let writeScreens gameDescriptor world =
                let screens = World.getScreens world
                World.writeScreens screens gameDescriptor world
            World.writeGame3 writeScreens gameDescriptor world

        /// Write a game to a file.
        static member writeGameToFile (filePath : string) world =
            let filePathTmp = filePath + ".tmp"
            let gameDescriptor = World.writeGame GameDescriptor.empty world
            let gameDescriptorStr = scstring gameDescriptor
            let gameDescriptorPretty = Symbol.prettyPrint String.Empty gameDescriptorStr
            File.WriteAllText (filePathTmp, gameDescriptorPretty)
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read a game from a game descriptor.
        static member readGame gameDescriptor world =
            World.readGame3 World.readScreens gameDescriptor world

        /// Read a game from a file.
        static member readGameFromFile (filePath : string) world =
            let gameDescriptorStr = File.ReadAllText filePath
            let gameDescriptor = scvalue<GameDescriptor> gameDescriptorStr
            World.readGame gameDescriptor world

namespace Debug
open Nu
type Game =

    /// Provides a full view of all the properties of a game. Useful for debugging such as with the
    /// Watch feature in Visual Studio.
    static member view world = World.viewGameProperties world