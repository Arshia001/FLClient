using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class MenuGroup<TIMenu> where TIMenu : IMenu
{
    Dictionary<Type, TIMenu> menus = new Dictionary<Type, TIMenu>();
    GameObject gameObject;

    public MenuGroup(GameObject gameObject)
    {
        this.gameObject = gameObject;
        foreach (var menu in gameObject.GetComponentsInChildren<TIMenu>())
            menus.Add(menu.GetType(), menu);
    }

    public virtual Task HideAll() => Task.WhenAll(menus.Values.Select(m => m.Hide()));

    public T Menu<T>() where T : MonoBehaviour, TIMenu => menus.TryGetValue(typeof(T), out var result) ? (T)result : null;

    public virtual Task Show<T>() where T : MonoBehaviour, TIMenu =>
        ShowCustom<T>(m => m.Show());

    public virtual async Task ShowCustom<T>(Action<T> f) where T : MonoBehaviour, TIMenu
    {
        gameObject.SetActive(true);
        await HideAll();
        f(Menu<T>());
    }

    public async Task Hide()
    {
        await HideAll();
        gameObject.SetActive(false);
    }
}
