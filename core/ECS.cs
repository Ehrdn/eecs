using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace EECS;

/// <summary>
/// World and World management.
/// </summary>
public class World
{
	/// <summary>
	/// Default / Current World for any method that requires a World but has an version that omits World for simplicity.
	/// </summary>
	public static World? DefaultWorld { get; set; }
	public static int DefaultMaxEntityID { get; set; } = 100000;
	public static int DefaultMaxEntityCount { get; set; } = 100000;

	public static int Count { get; private set; } = 0;
	
	/// <summary>
	/// World ID. Won't be reused. (What would you need 2.1B World for anyway??)
	/// </summary>
	public int ID { get; set; }
	public string Name { get; private set; }
	
	public EntitiesManager Entities;
	public ComponentsManager Components;

	public World(string name, int maxEntityID, int maxEntityCount)
	{
		Name = name;
		ID = Count;
		Count++;
		Entities = new EntitiesManager(this);
		Components = new ComponentsManager(this, maxEntityID);
	}

	public World(string name) : this(name, DefaultMaxEntityID, DefaultMaxEntityCount) { }
}

[Component]
public struct Position
{
	public readonly int x, y;
}

