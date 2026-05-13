package org.game2048.entity;

import javax.persistence.AttributeConverter;
import javax.persistence.Converter;
import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeFormatterBuilder;
import java.time.format.DateTimeParseException;
import java.time.temporal.ChronoField;

@Converter
public class InstantStringConverter implements AttributeConverter<Instant, String> {
    private static final DateTimeFormatter SQL_TIMESTAMP = new DateTimeFormatterBuilder()
            .appendPattern("yyyy-MM-dd HH:mm:ss")
            .optionalStart()
            .appendFraction(ChronoField.NANO_OF_SECOND, 0, 6, true)
            .optionalEnd()
            .toFormatter();

    @Override
    public String convertToDatabaseColumn(Instant attribute) {
        return attribute == null ? null : SQL_TIMESTAMP.format(LocalDateTime.ofInstant(attribute, ZoneOffset.UTC));
    }

    @Override
    public Instant convertToEntityAttribute(String dbData) {
        if (dbData == null) {
            return null;
        }

        try {
            return Instant.parse(dbData);
        } catch (DateTimeParseException ignored) {
            return LocalDateTime.parse(dbData, SQL_TIMESTAMP).toInstant(ZoneOffset.UTC);
        }
    }
}
