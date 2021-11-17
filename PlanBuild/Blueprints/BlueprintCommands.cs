﻿using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Blueprints.Marketplace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PlanBuild.Blueprints
{
    internal class BlueprintCommands
    {
        public static void Init()
        {
            CommandManager.Instance.AddConsoleCommand(new GetLocalListCommand());
            CommandManager.Instance.AddConsoleCommand(new DeleteBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new PushBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new GetServerListCommand());
            CommandManager.Instance.AddConsoleCommand(new PullBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new ThumbnailBlueprintCommand());
            CommandManager.Instance.AddConsoleCommand(new ThumbnailAllCommand());
        }

        /// <summary>
        ///     Console command which outputs the local blueprint list
        /// </summary>
        private class GetLocalListCommand : ConsoleCommand
        {
            public override string Name => "bp.local";

            public override string Help => "Get the list of your local blueprints";

            public override void Run(string[] args)
            {
                BlueprintSync.GetLocalBlueprints();
                Console.instance.Print(BlueprintManager.LocalBlueprints.ToString());
            }
        }

        /// <summary>
        ///     Console command to delete a local blueprint
        /// </summary>
        private class DeleteBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.remove";

            public override string Help => "[blueprint_id] Remove a local blueprint";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print(
@$"Usage: {Name} [blueprint_id]
blueprint_name: ID of the blueprint according to bp.local");
                    return;
                }

                var id = args[0];
                if (BlueprintSync.RemoveLocalBlueprint(id))
                {
                    Console.instance.Print($"Removed blueprint {id}");
                }
            }
        }

        /// <summary>
        ///     Console command which uploads a blueprint to the server
        /// </summary>
        private class PushBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.push";

            public override string Help => "[blueprint_id] Upload a local blueprint to the current connected server";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print(
@$"Usage: {Name} [blueprint_id]
blueprint_name: ID of the blueprint according to bp.local");
                    return;
                }

                var id = args[0];
                BlueprintSync.PushLocalBlueprint(id, (bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not upload blueprint: {message}");
                    }
                    else
                    {
                        Console.instance.Print($"Blueprint {id} uploaded");
                    }
                });
            }
        }

        /// <summary>
        ///     Console command which queries and outputs the server blueprint list
        /// </summary>
        private class GetServerListCommand : ConsoleCommand
        {
            public override string Name => "bp.server";

            public override string Help => "Get the list of the current connected servers blueprints";

            public override void Run(string[] args)
            {
                BlueprintSync.GetServerBlueprints((bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not get server list: {message}");
                    }
                    else
                    {
                        Console.instance.Print(BlueprintManager.ServerBlueprints.ToString());
                    }
                }, useCache: args.Length == 0
                );
            }
        }

        /// <summary>
        ///     Console command which queries and saves a named blueprint from the server
        /// </summary>
        private class PullBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.pull";

            public override string Help => "[blueprint_id] Load a blueprint from the current connected server and add it to your local blueprints";

            public override void Run(string[] args)
            {
                if (args.Length != 1)
                {
                    Console.instance.Print(
@$"Usage: {Name} [blueprint_id]
blueprint_name: ID of the blueprint according to bp.server");
                    return;
                }

                var id = args[0];
                BlueprintSync.GetServerBlueprints((bool success, string message) =>
                {
                    if (!success)
                    {
                        Console.instance.Print($"Could not load blueprint: {message}");
                    }
                    else if (BlueprintSync.SaveServerBlueprint(id))
                    {
                        Console.instance.Print($"Loaded blueprint {id} from server");
                    }
                });
            }
        }

        /// <summary>
        ///     Console command to generate a new thumbnail for a blueprint via Jötunn's RenderManager
        /// </summary>
        private class ThumbnailBlueprintCommand : ConsoleCommand
        {
            public override string Name => "bp.thumbnail";

            public override string Help => "[blueprint_id] ([rotation]) Create a new thumbnail for a blueprint from the actual blueprint data";

            public override void Run(string[] args)
            {
                if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
                {
                    Console.instance.Print(
@$"Usage: {Name} [blueprint_id] ([rotation])
blueprint_name: ID of the blueprint according to bp.local
rotation: Rotation on the Y-Axis in degrees (default: 0)");
                    return;
                }

                var id = args[0];
                if (!BlueprintManager.LocalBlueprints.TryGetValue(id, out var bp))
                {
                    Console.instance.Print($"Blueprint {id} not found");
                    return;
                }

                int additionalRot = 0;
                if (args.Length > 1 && int.TryParse(args[1], out int rot))
                {
                    additionalRot = rot;
                }

                var success = bp.CreateThumbnail(additionalRot);
                Console.instance.Print(success
                    ? $"Created thumbnail for {id}"
                    : $"Could not create thumbnail for {id}");
            }

            public override List<string> CommandOptionList()
            {
                return BlueprintManager.LocalBlueprints.Keys.ToList();
            }
        }

        /// <summary>
        ///     Console command to generate a new thumbnail for all current local blueprints
        /// </summary>
        private class ThumbnailAllCommand : ConsoleCommand
        {
            public override string Name => "bp.regenthumbnails";

            public override string Help => "Create a new thumbnail for all local blueprints";

            public override void Run(string[] args)
            {
                IEnumerator RegenAll()
                {
                    foreach (var bp in BlueprintManager.LocalBlueprints.Values)
                    {
                        yield return null;
                        bp.CreateThumbnail();
                    }
                    
                    Console.instance.Print("Thumbnails regenerated");
                }

                PlanBuildPlugin.Instance.StartCoroutine(RegenAll());
            }
        }
    }
}