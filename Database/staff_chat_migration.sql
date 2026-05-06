-- ============================================================
-- WildNest Staff Chat - DB Migration (safe, additive only)
-- Run once against wildnest_db.
-- Preserves tbl_staffmessages and adds only defensive support.
-- ============================================================

USE wildnest_db;

CREATE TABLE IF NOT EXISTS tbl_staffmessages (
    MessageID    INT          AUTO_INCREMENT PRIMARY KEY,
    SenderRole   VARCHAR(50)  NOT NULL,
    SenderName   VARCHAR(100) NOT NULL,
    ReceiverRole VARCHAR(50)  NOT NULL,
    Message      TEXT         NOT NULL,
    SentAt       DATETIME     DEFAULT NOW(),
    IsRead       BOOLEAN      DEFAULT FALSE
);

SET @col_exists = (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = 'wildnest_db'
      AND TABLE_NAME   = 'tbl_staffmessages'
      AND COLUMN_NAME  = 'IsRead'
);

SET @sql = IF(
    @col_exists = 0,
    'ALTER TABLE tbl_staffmessages ADD COLUMN IsRead BOOLEAN DEFAULT FALSE;',
    'SELECT ''IsRead column already present'' AS status;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE INDEX IF NOT EXISTS idx_staff_msg_thread
    ON tbl_staffmessages (SenderRole, ReceiverRole, SentAt);

CREATE INDEX IF NOT EXISTS idx_staff_msg_unread
    ON tbl_staffmessages (ReceiverRole, IsRead);

SELECT 'WildNest staff chat migration complete.' AS result;
