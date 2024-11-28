namespace Asv.IO;

public interface IStatisticHandler:IStatistic
{
    void AddRxBytes(int size);
    void AddTxBytes(int size);
    void IncrementRxMessage();
    void IncrementTxMessage();
    void IncrementRxError();
    void IncrementTxError();
    void AddParserBytes(int size);
    void IncrementParserUnknownMessageError();
    void IncrementParserBadCrcError();
    void IncrementParserDeserializeError();
    void IncrementParsedMessage();
    void IncrementParserReadNotAllDataError();
    void IncrementParserPublishError();

    void IncrementDropRxMessage();
    void IncrementDropTxMessage();
}