using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ImageLoader : MonoBehaviour
{
    public GameObject imagePrefab; // Drag your Image prefab here in Inspector
    public Transform imageParent; // Drag the parent GameObject here in Inspector

    void Start()
    {
        LoadImages();
        FirestoreController.OnNewImageDownloaded += LoadImage; // Subscribe to the event
    }

    void OnDestroy()
    {
        FirestoreController.OnNewImageDownloaded -= LoadImage; // Unsubscribe from the event
    }

    void LoadImages()
    {
        string[] files = Directory.GetFiles(Application.persistentDataPath, "image_*.png");
        foreach (string file in files)
        {
            LoadImage(file);
        }
    }

    void LoadImage(string filePath)
    {
        byte[] imageData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);

        GameObject imageObject = Instantiate(imagePrefab, imageParent);
        RawImage image = imageObject.GetComponent<RawImage>(); // Change Image to RawImage
        image.texture = tex; // Change sprite to texture
    }
}
