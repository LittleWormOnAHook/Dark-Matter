using System;
using Project.Data;
using UnityEngine;

namespace Project.Crafting
{
    [Serializable]
    public class RecipeIngredient
    {
        public ItemData item;
        public int amount = 1;
    }
}
