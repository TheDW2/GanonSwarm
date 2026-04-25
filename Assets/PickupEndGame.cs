using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FloatingPickupRestart : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text messageText;

    [Header("Pickup")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float restartDelay = 3f;

    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.25f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 startPosition;
    private bool pickedUp = false;

    private void Start()
    {
        startPosition = transform.position;

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (pickedUp) return;

        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pickedUp) return;

        if (other.CompareTag(playerTag))
        {
            pickedUp = true;

            if (messageText != null)
            {
                messageText.text = "Ganondorf Wins.";
                messageText.gameObject.SetActive(true);
            }

            gameObject.SetActive(false);
            StartCoroutine(RestartGameAfterDelay());
        }
    }

    private IEnumerator RestartGameAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}