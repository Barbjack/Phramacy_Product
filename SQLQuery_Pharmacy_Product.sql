USE [ReactDB]
GO

/****** Object:  Table [dbo].[Pharma_Medicines]    Script Date: 18-08-2025 18:53:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Pharma_Medicines](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](150) NOT NULL,
	[Batch] [nvarchar](50) NULL,
	[price] [float] NOT NULL,
	[Expiry] [datetime] NULL,
	[Quantity] [int] NULL,
	[QtyInLoose] [int] NULL,
	[Is_discontinued] [bit] NOT NULL,
	[manufacturer_name] [nvarchar](max) NOT NULL,
	[type] [nvarchar](50) NOT NULL,
	[pack_size_label] [nvarchar](100) NOT NULL,
	[short_composition1] [nvarchar](100) NOT NULL,
	[short_composition2] [nvarchar](100) NULL,
	[UpdatedAt] [datetime] NULL,
	[IsDeleted] [bit] NOT NULL,
	[Discount] [decimal](5, 2) NULL,
	[GST] [decimal](5, 2) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Pharma_Medicines] ADD  CONSTRAINT [DF_Pharma_Medicines_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO

