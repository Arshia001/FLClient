using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IMenu
{
    void Show();

    Task Hide();
}
