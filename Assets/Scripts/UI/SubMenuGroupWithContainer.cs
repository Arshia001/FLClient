using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SubMenuGroupWithContainer<T> : MenuGroup<T> where T : IMenu
{
    GameObject container;

    public SubMenuGroupWithContainer(GameObject container) : base(container) =>
        this.container = container;

    public override async Task Show<TMenu>()
    {
        await base.Show<TMenu>();
        container.SetActive(true);
    }

    public override async Task HideAll()
    {
        await base.HideAll();
        container.SetActive(false);
    }
}
