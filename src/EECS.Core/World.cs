using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;

using EECS.Core.Components;

namespace EECS.Core;

/// <summary>
/// World and World management.
/// </summary>
public partial class World
{
	public static int DefaultMaxEntityCount { get; set; } = 1000000;

	public static int Count { get; private set; } = 0;
	
	/// <summary>
	/// World ID. Won't be reused. (What would you need 2.1B World for anyway??)
	/// </summary>
	public int ID { get; set; }	
	public readonly EntitiesManager Entities;
	public readonly ComponentsManager Components;

	public World(int maxEntityCount)
	{
		ID = Count++;
		Entities = new EntitiesManager(this, maxEntityCount);
		Components = new ComponentsManager(this, maxEntityCount);
	}

	public World() : this(DefaultMaxEntityCount) { }

	public virtual void Reset()
	{
		Entities.Reset();
		Components.Reset();
	}
}

