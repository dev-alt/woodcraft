"""Unit conversion utilities for woodworking dimensions."""

from enum import Enum
from typing import TypeAlias

Number: TypeAlias = int | float


class Units(str, Enum):
    """Supported unit systems."""

    INCHES = "inches"
    MILLIMETERS = "mm"
    CENTIMETERS = "cm"
    FEET = "feet"


# Conversion factors to inches (base unit)
_TO_INCHES: dict[Units, float] = {
    Units.INCHES: 1.0,
    Units.MILLIMETERS: 1 / 25.4,
    Units.CENTIMETERS: 1 / 2.54,
    Units.FEET: 12.0,
}


class UnitConverter:
    """Convert between woodworking measurement units."""

    @staticmethod
    def to_inches(value: Number, from_unit: Units) -> float:
        """Convert a value to inches."""
        return float(value) * _TO_INCHES[from_unit]

    @staticmethod
    def from_inches(value: Number, to_unit: Units) -> float:
        """Convert a value from inches to another unit."""
        return float(value) / _TO_INCHES[to_unit]

    @staticmethod
    def convert(value: Number, from_unit: Units, to_unit: Units) -> float:
        """Convert a value between any two units."""
        inches = UnitConverter.to_inches(value, from_unit)
        return UnitConverter.from_inches(inches, to_unit)

    @staticmethod
    def board_feet(length: Number, width: Number, thickness: Number, unit: Units = Units.INCHES) -> float:
        """Calculate board feet from dimensions.

        Board feet = (length × width × thickness) / 144 (when in inches)
        """
        length_in = UnitConverter.to_inches(length, unit)
        width_in = UnitConverter.to_inches(width, unit)
        thickness_in = UnitConverter.to_inches(thickness, unit)
        return (length_in * width_in * thickness_in) / 144

    @staticmethod
    def linear_feet(length: Number, unit: Units = Units.INCHES) -> float:
        """Convert length to linear feet."""
        length_in = UnitConverter.to_inches(length, unit)
        return length_in / 12

    @staticmethod
    def square_feet(length: Number, width: Number, unit: Units = Units.INCHES) -> float:
        """Calculate square feet from dimensions."""
        length_in = UnitConverter.to_inches(length, unit)
        width_in = UnitConverter.to_inches(width, unit)
        return (length_in * width_in) / 144

    @staticmethod
    def format_fraction(value: float, precision: int = 16) -> str:
        """Format a decimal value as a fraction (e.g., 3/4, 1/2).

        Args:
            value: Decimal value to format
            precision: Denominator precision (8, 16, 32, 64)

        Returns:
            String representation with fraction (e.g., "3 1/2" or "0.75")
        """
        whole = int(value)
        frac = value - whole

        if frac < 1 / (precision * 2):
            return str(whole) if whole else "0"

        # Find closest fraction
        numerator = round(frac * precision)
        if numerator == precision:
            return str(whole + 1)

        # Simplify fraction
        from math import gcd

        divisor = gcd(numerator, precision)
        numerator //= divisor
        denominator = precision // divisor

        if whole:
            return f"{whole} {numerator}/{denominator}"
        return f"{numerator}/{denominator}"

    @staticmethod
    def parse_fraction(text: str) -> float:
        """Parse a fractional string to decimal.

        Accepts formats like: "3 1/2", "3/4", "2.5", "2"
        """
        text = text.strip()

        # Handle pure decimal
        if "/" not in text:
            return float(text)

        # Handle mixed number (e.g., "3 1/2")
        if " " in text:
            whole_str, frac_str = text.split(" ", 1)
            whole = float(whole_str)
        else:
            whole = 0.0
            frac_str = text

        # Parse fraction
        num_str, denom_str = frac_str.split("/")
        frac = float(num_str) / float(denom_str)

        return whole + frac
