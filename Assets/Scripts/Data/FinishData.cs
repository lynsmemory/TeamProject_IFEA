using UnityEngine;

public enum FinishType
{
    Wall,
    Floor,
    Ceiling,
    Lighting,
    Blind,
    Curtain
}

public sealed class FinishData : ScriptableObject
{
    // 마감재 데이터
    // 변수
    // - productName: string
    // - productImage: Sprite
    // - unitPrice: float
    // - thickness: float
    // - thermalConductivity: float
    // - material: Material

    // 함수
    // + GetProductName(): string
    // + GetProductImage(): Sprite
    // + GetUnitPrice(): float
    // + GetThickness(): float
    // + GetThermalConductivity(): float
    // + GetMaterial(): Material
    // + GetThermalResistance(): float
}
