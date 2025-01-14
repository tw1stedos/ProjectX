using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeController : MonoBehaviour
{
    private bool isSwiping = false;
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    isSwiping = true;
                    startTouchPosition = touch.position;
                    break;

                case TouchPhase.Ended:
                    isSwiping = false;
                    endTouchPosition = touch.position;
                    CheckSwipeDirection();
                    break;
            }
        }
    }

    private void CheckSwipeDirection()
    {
        Vector2 swipeDelta = endTouchPosition - startTouchPosition;

        if (swipeDelta.magnitude < 50f) return; // Игнорируем слишком короткие свайпы

        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            if (swipeDelta.x > 0)
            {
                HandleSwipe(true); // Свайп вправо - полезные предметы
            }
            else
            {
                HandleSwipe(false); // Свайп влево - мусор
            }
        }
    }

    private void HandleSwipe(bool isRightSwipe)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Trash") && !isRightSwipe)
            {
                ScoreManager.Instance.AddScore(10);
                PlaySound("TrashSwipe");
                Destroy(hitObject);
            }
            else if (hitObject.CompareTag("UsefulItem") && isRightSwipe)
            {
                ScoreManager.Instance.AddScore(20);
                PlaySound("UsefulItemSwipe");
                Destroy(hitObject);
            }
        }
    }

    private void PlaySound(string soundName)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        AudioClip clip = Resources.Load<AudioClip>(soundName);
        audioSource.PlayOneShot(clip);
    }
}