using System.Collections.Generic;
using UnityEngine;

public class AnchoredObject : MonoBehaviour
{
    [SerializeField] List<Anchor> anchors = new List<Anchor>();

    /// <summary>
    /// Get an anchor from its ID (index)
    /// </summary>
    /// <param name="anchorID">ID of the anchor to search for</param>
    /// <returns>The anchor that has the given ID</returns>
    public Anchor GetAnchorFromID(int anchorID)
    {
        if (anchorID > anchors.Count)
        {
            Debug.LogError("Error: Tried to anchor to an inexisting anchor. Check anchors list.");
        }
        return anchors[anchorID];
    }

    /// <summary>
    /// Anchor this object to another
    /// </summary>
    /// <param name="partner">The anchored object to connect to</param>
    /// <param name="partnerAnchorID">ID of the partner anchor to connect to</param>
    /// <param name="myAnchorID">ID of anchor this object wants to connect</param>
    /// <param name="matchScales">Whether to anchor scales too</param>
    public void AnchorTo(AnchoredObject partner, int partnerAnchorID, int myAnchorID, bool matchScales=true)
    {
        AnchorTo(
            GetAnchorFromID(myAnchorID),
            partner.GetAnchorFromID(partnerAnchorID)
        );
    }

    /// <summary>
    /// Anchor this object to another
    /// </summary>
    /// <param name="myAnchor">The anchor to connect</param>
    /// <param name="partnerAnchor">The anchor of the other object to connect to</param>
    /// <param name="matchScales">Whether to anchor scales too</param>
    public void AnchorTo(Anchor myAnchor, Anchor partnerAnchor, bool matchScales=true)
    {
        if (matchScales)
        {
            // Match scales
            float sizeDelta = partnerAnchor.transform.lossyScale.x / myAnchor.transform.lossyScale.x;
            transform.localScale *= sizeDelta;
        }

        // Match rotations
        transform.rotation =
            partnerAnchor.transform.rotation
            * Quaternion.AngleAxis(180f, Vector3.up)
            * Quaternion.Inverse(myAnchor.transform.rotation)
            * transform.rotation;

        // Match positions
        transform.position += partnerAnchor.transform.position - myAnchor.transform.position;
    }

    /// <summary>
    /// Connect multiple anchors of this object with multiple anchors.
    /// If one of the connections fails, the anchoring operation fails.
    /// </summary>
    /// <param name="partner">The anchored object to connect to</param>
    /// <param name="partnerAnchorIDs">IDs of the partner anchors to connect to</param>
    /// <param name="myAnchorIDs">IDs of the anchors this object wants to connect</param>
    /// <param name="matchScales">Whether to anchor scales too</param>
    /// <returns>Whether the anchoring succeeded or not</returns>
    public bool AnchorToMultiple(AnchoredObject partner, IList<int> partnerAnchorIDs, IList<int> myAnchorIDs, bool matchScales=true)
    {
        if (myAnchorIDs.Count != partnerAnchorIDs.Count)
        {
            Debug.LogError("Error: Number of anchors must match.");
        }
        List<Anchor> myAnchors = new List<Anchor>();
        List<Anchor> partnerAnchors = new List<Anchor>();
        for (int i = 0; i < myAnchorIDs.Count; i++)
        {
            myAnchors.Add(GetAnchorFromID(myAnchorIDs[i]));
            partnerAnchors.Add(partner.GetAnchorFromID(partnerAnchorIDs[i]));
        }
        return AnchorToMultiple(myAnchors, partnerAnchors, matchScales);
    }

    /// <summary>
    /// Connect multiple anchors of this object with multiple anchors.
    /// If one of the connections fails, the anchoring operation fails.
    /// </summary>
    /// <param name="myAnchors">The anchors this object wants to connect</param>
    /// <param name="targetAnchors">The anchors this object wants to connect to</param>
    /// <param name="matchScales">Whether to anchor scales too</param>
    /// <returns>Whether the anchoring succeeded or not</returns>
    public bool AnchorToMultiple(IList<Anchor> myAnchors, IList<Anchor> targetAnchors, bool matchScales=true)
    {
        if (myAnchors.Count != targetAnchors.Count)
        {
            Debug.LogError("Error: Number of anchors must match.");
            return false;
        }
        if (myAnchors.Count == 0)
        {
            Debug.LogError("Amount of anchors to connect must be at least 1.");
            return false;
        }

        // Connect the first anchors. Then, test if other anchors match.
        AnchorTo(myAnchors[0], targetAnchors[0], matchScales);
        for (int i=1; i<myAnchors.Count; i++)
        {
            if (! myAnchors[i].IsAnchoredTo(targetAnchors[i], matchScales))
            {
                return false;
            }
        }
        return true;
    }
}
