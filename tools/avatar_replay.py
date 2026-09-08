"""Offline pose/skinning audit. Standard library only; never modifies an input log.

Usage: python tools/avatar_replay.py LOG [LOG ...] [--vertices FULL_VERTEX_LOG]
"""
import argparse
import itertools
import json
import math
import re
from pathlib import Path


def numbers(value):
    return [float(x) for x in re.findall(r"[-+]?(?:\d*\.\d+|\d+)(?:[Ee][-+]?\d+)?", value)]


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def sub(a, b):
    return [x-y for x, y in zip(a, b)]


def unit(v):
    length = math.sqrt(dot(v, v))
    return [x/length for x in v]


def angle(a, b):
    return math.degrees(math.acos(max(-1, min(1, dot(unit(a), unit(b))))))


def qmul(a, b):
    x,y,z,w = a; X,Y,Z,W = b
    return [w*X+x*W+y*Z-z*Y, w*Y-x*Z+y*W+z*X,
            w*Z+x*Y-y*X+z*W, w*W-x*X-y*Y-z*Z]


def qinv(q):
    n = dot(q,q)
    return [-q[0]/n, -q[1]/n, -q[2]/n, q[3]/n]


def rotate(v, q):
    return qmul(qmul(q, list(v)+[0]), qinv(q))[:3]


def multiply(a,b):
    return [sum(a[r*4+k]*b[k*4+c] for k in range(4)) for r in range(4) for c in range(4)]


def transform(v,m):
    return [sum((list(v)+[1])[k]*m[4*k+c] for k in range(4)) for c in range(3)]


def pose(q,p):
    axes = [rotate(v,q)+[0] for v in ([1,0,0],[0,1,0],[0,0,1])]
    return sum(axes, []) + list(p)+[1]


def inverse(m):
    rows = [m[4*r:4*r+4]+[float(r==c) for c in range(4)] for r in range(4)]
    for c in range(4):
        k = max(range(c,4), key=lambda k:abs(rows[k][c]))
        rows[c],rows[k] = rows[k],rows[c]
        d=rows[c][c]
        assert abs(d)>1e-12
        rows[c]=[x/d for x in rows[c]]
        for r in range(4):
            if r!=c:
                d=rows[r][c];rows[r]=[x-d*y for x,y in zip(rows[r],rows[c])]
    return [x for row in rows for x in row[4:]]


def field(block,label,opening,closing):
    return numbers(re.search(re.escape(label)+re.escape(opening)+"([^"+re.escape(closing)+"]+)"+re.escape(closing),block)[1])


def read(path):
    text=Path(path).read_text(encoding="utf-8-sig")
    joints={};tracking={};palette={};vertices=[]
    for b in re.split(r"(?=^AVATAR JOINT )",text,flags=re.M)[1:]:
        path=re.search(r"AVATAR JOINT (.*?) parent=",b)[1];name=path.split('/')[-1]
        joints[name]={"parent":path.split('/')[-2],"bind":field(b,"  initialWorld=","[","]"),
                      "world":field(b,"  world=","[","]"),"q":field(b,"  worldOrientation=","(",")")}
    for b in re.split(r"(?=^TRACKING )",text,flags=re.M)[1:]:
        name=re.search(r"expected=(\S+)",b)[1]
        if name=="<unmapped>":continue
        tracking[name]={"p":numbers(re.search(r"flags=.*?position=<([^>]+)>",b)[1]),
                        "q":field(b,"  tracking.orientation=","(",")"),
                        "aq":field(b,"  cachedAvatarBind=","(",")")}
    for b in re.split(r"(?=^  PALETTE )",text,flags=re.M)[1:]:
        i=int(re.search(r"PALETTE \[(\d+)\]",b)[1]);name=re.search(r"joint=(.*)",b)[1].strip().split('/')[-1]
        palette[i]=(name,field(b,"    inverseBind=","[","]"))
    for m in re.finditer(r"V\[\d+\] position=<([^>]+)> joints=\(([^)]+)\) weights=<([^>]+)>",text):
        vertices.append((numbers(m[1]),[int(x) for x in m[2].split(',')],numbers(m[3])))
    return joints,tracking,palette,vertices


SEGMENTS=[("shoulder_left_joint","elbow_left_joint"),("shoulder_right_joint","elbow_right_joint"),
          ("elbow_left_joint","handWrist_left_joint"),("elbow_right_joint","handWrist_right_joint"),
          ("handIndex_00_left_joint","handIndex_01_left_joint"),("handIndex_00_right_joint","handIndex_01_right_joint")]
ACROSS=[("chest_joint","shoulder_left_joint","shoulder_right_joint"),
        ("handWrist_left_joint","handIndex_00_left_joint","handPinky_00_left_joint"),
        ("handWrist_right_joint","handIndex_00_right_joint","handPinky_00_right_joint")]


def audit(path,vertices):
    joints,tracking,palette,_=read(path)
    # Change only translations, preserving each existing local rotation and scale.
    translated={}
    for name,j in joints.items():
        parent=j["parent"]
        local=multiply(j["world"],inverse(joints[parent]["world"])) if parent in joints else j["world"][:]
        world=multiply(local,translated[parent]) if parent in translated else local[:]
        if name in tracking:world[12:15]=tracking[name]["p"]
        translated[name]=world
    rotation_error=max(abs(translated[n][4*r+c]-j['world'][4*r+c]) for n,j in joints.items() for r in range(3) for c in range(3))
    assert rotation_error<1e-9,rotation_error
    oldskin={i:multiply(ib,joints[n]['world']) for i,(n,ib) in palette.items()}
    newskin={i:multiply(ib,translated[n]) for i,(n,ib) in palette.items()}
    skin_error=max(abs(oldskin[i][4*r+c]-newskin[i][4*r+c]) for i in palette for r in range(3) for c in range(3))
    assert skin_error<1e-9,skin_error
    maxvertex=0
    for v,ids,weights in vertices:
        old=[0,0,0];new=[0,0,0]
        for i,w in zip(ids,weights):
            if not w:continue
            a=transform(v,oldskin[i]);b=transform(v,newskin[i])
            for k in range(3):old[k]+=w*a[k];new[k]+=w*b[k]
        maxvertex=max(maxvertex,math.sqrt(dot(sub(old,new),sub(old,new))))
    # Exhaustively classify the 24 proper signed axis permutations. This is a
    # coordinate-basis test, not a per-frame/per-bone fit or pose calibration.
    candidates=[]
    constraints=[]
    for root,a,b in [(a,a,b) for a,b in SEGMENTS]+ACROSS:
        if any(n not in tracking for n in (root,a,b)):continue
        local=rotate(sub(joints[b]['bind'][12:15],joints[a]['bind'][12:15]),qinv(tracking[root]['aq']))
        target=rotate(sub(tracking[b]['p'],tracking[a]['p']),qinv(tracking[root]['q']))
        constraints.append((root+' : '+a+' -> '+b,local,target))
    for perm in itertools.permutations(range(3)):
        parity=(-1)**sum(perm[i]>perm[j] for i in range(3) for j in range(i+1,3))
        for signs in itertools.product((-1,1),repeat=3):
            if parity*math.prod(signs)!=1:continue
            errors=[angle([signs[k]*v[perm[k]] for k in range(3)],target) for _,v,target in constraints]
            candidates.append((sum(errors)/len(errors),perm,signs,errors))
    candidates.sort()
    best=candidates[0]
    return {"log":str(path),"joints":len(joints),"vertices_replayed":len(vertices),
            "translation_only_max_joint_rotation_change":rotation_error,
            "translation_only_max_skin_rotation_change":skin_error,
            "translation_only_max_vertex_displacement_m":maxvertex,
            "best_fixed_axis_conversion":{"permutation":best[1],"signs":best[2],"mean_error_degrees":best[0]},
            "runner_up_mean_error_degrees":candidates[1][0],
            "fixed_conversion_constraints_degrees":{n:e for (n,_,_),e in zip(constraints,best[3])}}


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('logs',nargs='+');parser.add_argument('--vertices')
    args=parser.parse_args()
    vertices=read(args.vertices)[3] if args.vertices else []
    print(json.dumps([audit(p,vertices) for p in args.logs],indent=2))
