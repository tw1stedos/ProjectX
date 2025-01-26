using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextImageCompleted : MonoBehaviour
{
	public Sprite[] pictures;

	// Use this for initialization
	void OnEnable()
	{

		var backgroundSpriteNum = Random.Range(0, 4);
		if (backgroundSpriteNum == 0)
		{
			GetComponent<Image>().sprite = pictures[backgroundSpriteNum];
			GetComponent<Image>().SetNativeSize();
		}

		else if (backgroundSpriteNum == 1)
		{
			GetComponent<Image>().sprite = pictures[backgroundSpriteNum];
			GetComponent<Image>().SetNativeSize();
		}
		else if (backgroundSpriteNum == 2)
		{
			GetComponent<Image>().sprite = pictures[backgroundSpriteNum];
			GetComponent<Image>().SetNativeSize();
		}
		else if (backgroundSpriteNum == 3)
		{
			GetComponent<Image>().sprite = pictures[backgroundSpriteNum];
			GetComponent<Image>().SetNativeSize();
		}
		else if (backgroundSpriteNum == 4)
		{
			GetComponent<Image>().sprite = pictures[backgroundSpriteNum];
			GetComponent<Image>().SetNativeSize();
		}
	}
}