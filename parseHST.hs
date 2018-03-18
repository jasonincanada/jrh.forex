{- 
   parseHST.hs 

     Desc: Parse a MetaTrader 4 history file (eg: AUDUSD60.hst)
   Author: Jason Hooper
     Date: 18 March 2018
     
    Usage: $ runghc parseHST < hst/AUDUSD60.hst
           ("AUDUSD-60",0.8135,0.75008)

  Remarks: The mql4 forum sites are down as of this revision, an archived
           version of the .hst file spec is here:

           http://web.archive.org/web/20160119203939/http://forum.mql4.com:80/60455

           This module supports format version 401 only.
-}

import qualified Data.ByteString.Lazy as BL
import Data.Binary.Get
import Data.Char (chr)
import Data.List (maximumBy, minimumBy)
import Data.Word

data HSTHeader = HSTHeader { version      :: Word32,
                             copyright    :: String,
                             symbol       :: String,
                             period       :: Word32,
                             digits       :: Word32,
                             dateCreated  :: Word32,
                             dateLastSync :: Word32
                           } deriving (Show)

data HSTBar = HSTBar { timestamp :: Word64,
                       open      :: Double,
                       high      :: Double,
                       low       :: Double,
                       close     :: Double,
                       volume    :: Word64,
                       spread    :: Word32,
                       realVol   :: Word64
                     } deriving (Show)

-- Convert to string, excluding null bytes
toString :: BL.ByteString -> String
toString = map chr
            . filter (/=0)
            . map fromEnum
            . BL.unpack

-- Parse the header
getHeader :: Get HSTHeader
getHeader = do
  version      <- getWord32le
  copyright    <- getLazyByteString 64
  symbol       <- getLazyByteString 12
  period       <- getWord32le
  digits       <- getWord32le
  dateCreated  <- getWord32le
  dateLastSync <- getWord32le
  getLazyByteString (13*4) -- Skip over reserved space
  return $ HSTHeader version (toString copyright) (toString symbol) period digits dateCreated dateLastSync

-- Parse a single bar of price data
getBar :: Get HSTBar
getBar = do
  timestamp <- getWord64le
  open      <- getDoublele
  low       <- getDoublele
  high      <- getDoublele
  close     <- getDoublele
  volume    <- getWord64le
  spread    <- getWord32le
  realVol   <- getWord64le
  return $ HSTBar timestamp open low high close volume spread realVol

-- Parse bars until the file runs out
getBars :: Get [HSTBar]
getBars = do
  empty <- isEmpty
  if empty
  then return []
  else do bar  <- getBar
          bars <- getBars
          return (bar : bars)

getHSTFile :: Get (HSTHeader, [HSTBar])
getHSTFile = do
  header <- getHeader
  bars   <- getBars
  return (header, bars)

main = do
  file <- BL.getContents 
  let (header, bars) = runGet getHSTFile file
  let maxPrice = maximumBy (\a b -> (high a) `compare` (high b)) bars
  let minPrice = minimumBy (\a b -> (low a) `compare` (low b)) bars
  print $ (symbol header ++ "-" ++ show (period header), high maxPrice, low minPrice)

