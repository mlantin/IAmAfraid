# automatically generated by the FlatBuffers compiler, do not modify

# namespace: Protocol

import flatbuffers

class Nugget(object):
    __slots__ = ['_tab']

    @classmethod
    def GetRootAsNugget(cls, buf, offset):
        n = flatbuffers.encode.Get(flatbuffers.packer.uoffset, buf, offset)
        x = Nugget()
        x.Init(buf, n + offset)
        return x

    # Nugget
    def Init(self, buf, pos):
        self._tab = flatbuffers.table.Table(buf, pos)

    # Nugget
    def Scope(self):
        o = flatbuffers.number_types.UOffsetTFlags.py_type(self._tab.Offset(4))
        if o != 0:
            return self._tab.String(o + self._tab.Pos)
        return ""

    # Nugget
    def Origin(self):
        o = flatbuffers.number_types.UOffsetTFlags.py_type(self._tab.Offset(6))
        if o != 0:
            return self._tab.String(o + self._tab.Pos)
        return ""

    # Nugget
    def Type(self):
        o = flatbuffers.number_types.UOffsetTFlags.py_type(self._tab.Offset(8))
        if o != 0:
            return self._tab.Get(flatbuffers.number_types.Int8Flags, o + self._tab.Pos)
        return 0

    # Nugget
    def Flakes(self, j):
        o = flatbuffers.number_types.UOffsetTFlags.py_type(self._tab.Offset(10))
        if o != 0:
            x = self._tab.Vector(o)
            x += flatbuffers.number_types.UOffsetTFlags.py_type(j) * 4
            x = self._tab.Indirect(x)
            from .Flake import Flake
            obj = Flake()
            obj.Init(self._tab.Bytes, x)
            return obj
        return None

    # Nugget
    def FlakesLength(self):
        o = flatbuffers.number_types.UOffsetTFlags.py_type(self._tab.Offset(10))
        if o != 0:
            return self._tab.VectorLen(o)
        return 0

def NuggetStart(builder): builder.StartObject(4)
def NuggetAddScope(builder, scope): builder.PrependUOffsetTRelativeSlot(0, flatbuffers.number_types.UOffsetTFlags.py_type(scope), 0)
def NuggetAddOrigin(builder, origin): builder.PrependUOffsetTRelativeSlot(1, flatbuffers.number_types.UOffsetTFlags.py_type(origin), 0)
def NuggetAddType(builder, type): builder.PrependInt8Slot(2, type, 0)
def NuggetAddFlakes(builder, flakes): builder.PrependUOffsetTRelativeSlot(3, flatbuffers.number_types.UOffsetTFlags.py_type(flakes), 0)
def NuggetStartFlakesVector(builder, numElems): return builder.StartVector(4, numElems, 4)
def NuggetEnd(builder): return builder.EndObject()