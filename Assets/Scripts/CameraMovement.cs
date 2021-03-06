using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement"), SerializeField]
    float _moveSpeed = 3.0f;
    [SerializeField]
    float _rotationSpeed = 3.0f;

    [Header("FPS Display"), SerializeField]
    TextMeshProUGUI _fpsText;
    [SerializeField]
    float _refreshRate = 0.1f;
    float _currentRefreshRate;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _currentRefreshRate = _refreshRate;
    }

    private void Update()
    {
        // Framerate Display
        int fps = (int)(1f / Time.unscaledDeltaTime);
        if (_currentRefreshRate <= 0)
        {
            _currentRefreshRate = _refreshRate;
            _fpsText.text = Mathf.Ceil(fps).ToString();
        }
        _currentRefreshRate -= Time.unscaledDeltaTime;

        // Camera Movement
        // Forward
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * _moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * _moveSpeed * Time.deltaTime;
        }
        // Horizontal
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * _moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * _moveSpeed * Time.deltaTime;
        }
        // Horizontal
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += transform.up * _moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= transform.up * _moveSpeed * Time.deltaTime;
        }
        // Camera Rotation
        // X
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 rotation = Vector3.zero;
            if (Input.GetAxis("Mouse Y") > 0)
            {
                rotation.x = -_rotationSpeed * Time.deltaTime;
            }
            else if (Input.GetAxis("Mouse Y") < 0)
            {
                rotation.x = _rotationSpeed * Time.deltaTime;
            }
            // Y
            if (Input.GetAxis("Mouse X") > 0)
            {
                rotation.y = _rotationSpeed * Time.deltaTime;
            }
            else if (Input.GetAxis("Mouse X") < 0)
            {
                rotation.y = -_rotationSpeed * Time.deltaTime;
            }
            transform.eulerAngles += rotation;
        }
    }
}
