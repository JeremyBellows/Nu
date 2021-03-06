﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2012-2016.

namespace Nu
open System
open System.IO
open Prime
open Nu

[<AutoOpen>]
module WorldScreenModule =

    type Screen with
    
        member this.GetId world = World.getScreenId this world
        member this.Id = PropertyTag.makeReadOnly this Property? Id this.GetId
        member this.GetName world = World.getScreenName this world
        member this.Name = PropertyTag.makeReadOnly this Property? Name this.GetName
        member this.GetXtension world = World.getScreenXtension this world
        member this.Xtension = PropertyTag.makeReadOnly this Property? Xtension this.GetXtension
        member this.GetDispatcherNp world = World.getScreenDispatcherNp this world
        member this.DispatcherNp = PropertyTag.makeReadOnly this Property? DispatcherNp this.GetDispatcherNp
        member this.GetSpecialization world = World.getScreenSpecialization this world
        member this.Specialization = PropertyTag.makeReadOnly this Property? Specialization this.GetSpecialization
        member this.GetClassification world = Classification.make (getTypeName ^ this.GetDispatcherNp world) (this.GetSpecialization world)
        member this.Classification = PropertyTag.makeReadOnly this Property? Classification this.GetClassification
        member this.GetPersistent world = World.getScreenPersistent this world
        member this.SetPersistent value world = World.setScreenPersistent value this world
        member this.Persistent = PropertyTag.makeReadOnly this Property? Persistent this.GetPersistent
        member this.GetCreationTimeStampNp world = World.getScreenCreationTimeStampNp this world
        member this.CreationTimeStampNp = PropertyTag.makeReadOnly this Property? CreationTimeStampNp this.GetCreationTimeStampNp
        member this.GetEntityTreeNp world = World.getScreenEntityTreeNp this world
        member this.SetEntityTreeNp value world = World.setScreenEntityTreeNp value this world
        member this.EntityTreeNp = PropertyTag.makeReadOnly this Property? EntityTreeNp this.GetEntityTreeNp
        member this.GetTransitionStateNp world = World.getScreenTransitionStateNp this world
        member this.SetTransitionStateNp value world = World.setScreenTransitionStateNp value this world
        member this.TransitionStateNp = PropertyTag.makeReadOnly this Property? TransitionStateNp this.GetTransitionStateNp
        member this.GetTransitionTicksNp world = World.getScreenTransitionTicksNp this world
        member this.SetTransitionTicksNp value world = World.setScreenTransitionTicksNp value this world
        member this.TransitionTicksNp = PropertyTag.makeReadOnly this Property? TransitionTicksNp this.GetTransitionTicksNp
        member this.GetIncoming world = World.getScreenIncoming this world
        member this.SetIncoming value world = World.setScreenIncoming value this world
        member this.Incoming = PropertyTag.makeReadOnly this Property? Incoming this.GetIncoming
        member this.GetOutgoing world = World.getScreenOutgoing this world
        member this.SetOutgoing value world = World.setScreenOutgoing value this world
        member this.Outgoing = PropertyTag.makeReadOnly this Property? Outgoing this.GetOutgoing

        /// Get a property value and type.
        member this.GetProperty propertyName world = World.getScreenProperty propertyName this world

        /// Set a property value with explicit type.
        member this.SetProperty propertyName property world = World.setScreenProperty propertyName property this world

        /// Get a property value.
        member this.Get<'a> propertyName world : 'a = World.getScreenProperty propertyName this world |> fst :?> 'a

        /// Set a property value.
        member this.Set<'a> propertyName (value : 'a) world = World.setScreenProperty propertyName (value :> obj, typeof<'a>) this world

        /// Check that a screen is in an idling state (not transitioning in nor out).
        member this.IsIdling world = this.GetTransitionStateNp world = IdlingState

        /// Check that a screen dispatches in the same manner as the dispatcher with the target type.
        member this.DispatchesAs (dispatcherTargetType : Type) world = Reflection.dispatchesAs dispatcherTargetType (this.GetDispatcherNp world)

    type World with

        static member private removeScreen screen world =
            let removeGroups screen world =
                let groups = World.getGroups screen world
                World.destroyGroupsImmediate groups world
            World.removeScreen3 removeGroups screen world

        static member internal updateScreen (screen : Screen) world =
            World.withEventContext (fun world ->
                let dispatcher = World.getScreenDispatcherNp screen world
                let world = dispatcher.Update (screen, world)
                let eventTrace = EventTrace.record "World" "updateScreen" EventTrace.empty
                World.publish7 World.sortSubscriptionsByHierarchy () (Events.Update ->- screen) eventTrace Simulants.Game true world)
                (atooa screen.ScreenAddress)
                world

        static member internal postUpdateScreen (screen : Screen) world =
            World.withEventContext (fun world ->
                let dispatcher = World.getScreenDispatcherNp screen world
                let world = dispatcher.PostUpdate (screen, world)
                let eventTrace = EventTrace.record "World" "postUpdateScreen" EventTrace.empty
                World.publish7 World.sortSubscriptionsByHierarchy () (Events.PostUpdate ->- screen) eventTrace Simulants.Game true world)
                (atooa screen.ScreenAddress)
                world

        static member internal actualizeScreen (screen : Screen) world =
            World.withEventContext (fun world ->
                let dispatcher = screen.GetDispatcherNp world
                dispatcher.Actualize (screen, world))
                (atooa screen.ScreenAddress)
                world

        /// Get all the world's screens.
        static member getScreens world =
            Umap.fold
                (fun state _ (screenAddress, _) -> Screen.proxy screenAddress :: state)
                [] (World.getScreenDirectory world) :> _ seq

        /// Destroy a screen in the world immediately. Can be dangerous if existing in-flight publishing depends on the
        /// screen's existence. Consider using World.destroyScreen instead.
        static member destroyScreenImmediate screen world =
            World.removeScreen screen world

        /// Destroy a screen in the world at the end of the current update.
        static member destroyScreen screen world =
            let tasklet =
                { ScheduledTime = World.getTickTime world
                  Command = { Execute = fun world -> World.destroyScreenImmediate screen world }}
            World.addTasklet tasklet world

        /// Create a screen and add it to the world.
        static member createScreen4 dispatcherName specializationOpt nameOpt world =
            let dispatchers = World.getScreenDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None -> failwith ^ "Could not find ScreenDispatcher '" + dispatcherName + "'. Did you forget to expose this dispatcher from your NuPlugin?"
            let screenState = ScreenState.make specializationOpt nameOpt dispatcher
            let screenState = Reflection.attachProperties ScreenState.copy dispatcher screenState
            let screen = ntos screenState.Name
            let world = World.addScreen false screenState screen world
            (screen, world)

        /// Create a screen and add it to the world.
        static member createScreen<'d when 'd :> ScreenDispatcher> specializationOpt nameOpt world =
            World.createScreen4 typeof<'d>.Name specializationOpt nameOpt world
        
        /// Create a screen with a dissolving transition, and add it to the world.
        static member createDissolveScreen<'d when 'd :> ScreenDispatcher> dissolveData specializationOpt nameOpt world =
            let dissolveImageOpt = Some dissolveData.DissolveImage
            let (screen, world) = World.createScreen<'d> specializationOpt nameOpt world
            let world = screen.SetIncoming { Transition.make Incoming with TransitionLifetime = dissolveData.IncomingTime; DissolveImageOpt = dissolveImageOpt } world
            let world = screen.SetOutgoing { Transition.make Outgoing with TransitionLifetime = dissolveData.OutgoingTime; DissolveImageOpt = dissolveImageOpt } world
            (screen, world)

        /// Write a screen to a screen descriptor.
        static member writeScreen screen screenDescriptor world =
            let writeGroups screen screenDescriptor world =
                let groups = World.getGroups screen world
                World.writeGroups groups screenDescriptor world
            World.writeScreen4 writeGroups screen screenDescriptor world

        /// Write multiple screens to a game descriptor.
        static member writeScreens screens gameDescriptor world =
            screens |>
            Seq.sortBy (fun (screen : Screen) -> screen.GetCreationTimeStampNp world) |>
            Seq.filter (fun (screen : Screen) -> screen.GetPersistent world) |>
            Seq.fold (fun screenDescriptors screen -> World.writeScreen screen ScreenDescriptor.empty world :: screenDescriptors) gameDescriptor.Screens |>
            fun screenDescriptors -> { gameDescriptor with Screens = screenDescriptors }

        /// Write a screen to a file.
        static member writeScreenToFile (filePath : string) screen world =
            let filePathTmp = filePath + ".tmp"
            let screenDescriptor = World.writeScreen screen ScreenDescriptor.empty world
            let screenDescriptorStr = scstring screenDescriptor
            let screenDescriptorPretty = Symbol.prettyPrint String.Empty screenDescriptorStr
            File.WriteAllText (filePathTmp, screenDescriptorPretty)
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read a screen from a screen descriptor.
        static member readScreen screenDescriptor nameOpt world =
            World.readScreen4 World.readGroups screenDescriptor nameOpt world

        /// Read a screen from a file.
        static member readScreenFromFile (filePath : string) nameOpt world =
            let screenDescriptorStr = File.ReadAllText filePath
            let screenDescriptor = scvalue<ScreenDescriptor> screenDescriptorStr
            World.readScreen screenDescriptor nameOpt world

        /// Read multiple screens from a game descriptor.
        static member readScreens gameDescriptor world =
            Seq.foldBack
                (fun screenDescriptor (screens, world) ->
                    let (screen, world) = World.readScreen screenDescriptor None world
                    (screen :: screens, world))
                gameDescriptor.Screens
                ([], world)

namespace Debug
open Nu
type Screen =

    /// Provides a full view of all the member properties of a screen. Useful for debugging such
    /// as with the Watch feature in Visual Studio.
    static member view screen world = World.viewScreenProperties screen world