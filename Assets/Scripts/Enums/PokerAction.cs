
public enum PokerAction
{
    Fold = 0, //ui
    Check = 1, //ui
    Bet = 2, //input your bet
    Call = 3, //ui
    Raise = 4 //input your bet
}

// If nobody has yet made a bet, then a player may either check (decline to bet, but keep their cards) or bet.
// If a player has bet, then subsequent players can fold, call or raise. To call is to match the amount the previous player has bet.
// To raise is to not only match the previous bet, but to also increase it