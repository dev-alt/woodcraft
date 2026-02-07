"""Tests for the units module."""

import pytest
from woodcraft.utils.units import UnitConverter, Units


class TestUnitConverter:
    """Tests for UnitConverter class."""

    def test_to_inches_from_inches(self):
        assert UnitConverter.to_inches(10, Units.INCHES) == 10

    def test_to_inches_from_mm(self):
        result = UnitConverter.to_inches(25.4, Units.MILLIMETERS)
        assert abs(result - 1.0) < 0.001

    def test_to_inches_from_cm(self):
        result = UnitConverter.to_inches(2.54, Units.CENTIMETERS)
        assert abs(result - 1.0) < 0.001

    def test_to_inches_from_feet(self):
        assert UnitConverter.to_inches(1, Units.FEET) == 12

    def test_from_inches_to_mm(self):
        result = UnitConverter.from_inches(1, Units.MILLIMETERS)
        assert abs(result - 25.4) < 0.001

    def test_convert_mm_to_cm(self):
        result = UnitConverter.convert(100, Units.MILLIMETERS, Units.CENTIMETERS)
        assert abs(result - 10) < 0.001

    def test_board_feet(self):
        # 1" x 12" x 12" = 1 board foot
        bf = UnitConverter.board_feet(12, 12, 1, Units.INCHES)
        assert abs(bf - 1.0) < 0.001

    def test_board_feet_3_4_stock(self):
        # 3/4" x 6" x 8' = 3 board feet
        bf = UnitConverter.board_feet(96, 6, 0.75, Units.INCHES)
        assert abs(bf - 3.0) < 0.001

    def test_linear_feet(self):
        lf = UnitConverter.linear_feet(24, Units.INCHES)
        assert lf == 2.0

    def test_square_feet(self):
        sf = UnitConverter.square_feet(24, 24, Units.INCHES)
        assert abs(sf - 4.0) < 0.001

    def test_format_fraction_whole(self):
        assert UnitConverter.format_fraction(3.0) == "3"

    def test_format_fraction_half(self):
        assert UnitConverter.format_fraction(0.5) == "1/2"

    def test_format_fraction_quarter(self):
        assert UnitConverter.format_fraction(0.25) == "1/4"

    def test_format_fraction_three_quarters(self):
        assert UnitConverter.format_fraction(0.75) == "3/4"

    def test_format_fraction_mixed(self):
        assert UnitConverter.format_fraction(3.5) == "3 1/2"

    def test_parse_fraction_whole(self):
        assert UnitConverter.parse_fraction("3") == 3.0

    def test_parse_fraction_simple(self):
        assert UnitConverter.parse_fraction("1/2") == 0.5

    def test_parse_fraction_mixed(self):
        assert UnitConverter.parse_fraction("3 1/2") == 3.5

    def test_parse_fraction_decimal(self):
        assert UnitConverter.parse_fraction("2.5") == 2.5
