using UnityEngine;
using System.Collections;


public class CFiber_Demo : MonoBehaviour
{
    void Start()
    {
        CFiber.Instance.PlayCoroutine(TestCo());
    }

    IEnumerator TestCo()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Success! Wait For seconds" + Time.time);

        yield return new CustomWaitForMileSeconds(3000);
        Debug.Log("Success! Wait For mileseconds" + Time.time);

        Debug.Log("Over TestCo");
    }
}


public class CustomWaitForMileSeconds : CFiberBase
{
    private int MileSeconds;

    private float StartTime;

    public CustomWaitForMileSeconds(int mileseconds)
    {
        MileSeconds = mileseconds;
        StartTime = Time.time;
    }

    public override IEnumerator Wait()
    {
        float endTime = StartTime + (float)MileSeconds / 1000f;
        while (Time.time < endTime)
            yield return null;
    }

}
