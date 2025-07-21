using UnityEngine;

public class TempTTSTester : MonoBehaviour
{
    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            GetComponent<ElevenLabsTTS>().Speak("Hey Shrek, where we goin'?");
        }
    }
}
