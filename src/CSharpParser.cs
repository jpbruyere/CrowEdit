﻿using System;
using Crow;

namespace Crow.Coding
{
	public class CSharpParser : Parser
	{
		public new enum TokenType {
			Unknown = Parser.TokenType.Unknown,
			WhiteSpace = Parser.TokenType.WhiteSpace,
			LineComment = Parser.TokenType.LineComment,
			BlockComment = Parser.TokenType.BlockComment,
			OpenParenth,
			CloseParenth,
			OpenBlock,
			CloseBlock,
			StatementEnding,
			UnaryOp,
			BinaryOp,
			Affectation,
			StringLiteral,
			CharacterLiteral,
			DigitalLiteral,
			Literal,
			Identifier,
			Indexer,
			Type,
			Preprocessor,
		}

		public CSharpParser (CodeBuffer _buffer) : base(_buffer)
		{
		}

		public override void Parse (int line)
		{
			throw new NotImplementedException ();
		}
	}
}

