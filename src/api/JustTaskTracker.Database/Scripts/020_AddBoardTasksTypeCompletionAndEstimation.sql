ALTER TABLE [dbo].[BoardTasks]
ADD
    [Type]               TINYINT          NOT NULL DEFAULT (1),
    [IsDone]             BIT              NOT NULL DEFAULT (0),
    [CompletedAtUtc]     DATETIME2(7)     NULL,
    [StoryPoints]        TINYINT          NULL,
    [TimeboxHours]       SMALLINT         NULL;
GO

ALTER TABLE [dbo].[BoardTasks]
ADD CONSTRAINT [CK_BoardTasks_Completion] CHECK (
    ([IsDone] = 0 AND [CompletedAtUtc] IS NULL) OR
    ([IsDone] = 1 AND [CompletedAtUtc] IS NOT NULL));
GO

ALTER TABLE [dbo].[BoardTasks]
ADD CONSTRAINT [CK_BoardTasks_Estimation] CHECK (
    ([Type] = 1 AND [TimeboxHours] IS NULL
        AND ([StoryPoints] IS NULL OR [StoryPoints] IN (0, 1, 2, 3, 5, 8, 13, 21, 34)))
    OR ([Type] = 2 AND [StoryPoints] IS NULL AND [TimeboxHours] IS NULL)
    OR ([Type] = 3 AND [StoryPoints] IS NULL
        AND ([TimeboxHours] IS NULL OR [TimeboxHours] BETWEEN 1 AND 999)));
GO
