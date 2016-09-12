//+------------------------------------------------------------------+
//|                                                          jrh.mq4 |
//|                                                                  |
//|                                   Copyright © 2016, Jason Hooper |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers         0

string IndicatorID = "jrh_";

const string file = "jrh.forex.txt";

struct Channel
{      
   string symbol;
   string timeframe;
   string name;

   datetime startDate;
   double startPrice;
   
   datetime castDate;
   double castPrice;
   
   datetime supportDate;
   double supportPrice;
};

int init()
{
   IndicatorBuffers(0);
   
   return(0);
}

int deinit()
{
   DeleteAllObjects();
   
   return(0);
}

int start()
{
   // Lazy, could be improved to redraw only what needs to be redrawn
   DeleteAllObjects();
   PlotObjects();
   
   return(0);
}


void DeleteAllObjects()
{
   // Delete all objects created by the indicator
   for (int i = ObjectsTotal() - 1;  i >= 0;  i--)
   {
      string name = ObjectName(i);
      
      if (StringSubstr(name, 0, StringLen(IndicatorID)) == IndicatorID)
         ObjectDelete(name);
   }
}

void PlotObjects()
{
   int fh = FileOpen(file, FILE_READ);
   string str;
   
   if (fh != INVALID_HANDLE) 
   { 
      while (!FileIsEnding(fh))
      {
         str = FileReadString(fh);
         
         string parts[];
         
         int i = StringSplit(str, ' ', parts);
         
         if (parts[0] == "C")
         {
            Channel c;
            
            c.symbol = parts[1];
            c.timeframe = parts[2];
            c.name = parts[3];
            c.startDate = StringToTime(parts[4] + " " + parts[5]);
            c.startPrice = StringToDouble(parts[6]);
            c.castDate = StringToTime(parts[7] + " " + parts[8]);
            c.castPrice = StringToDouble(parts[9]);
            c.supportDate = StringToTime(parts[10] + " " + parts[11]);
            c.supportPrice = StringToDouble(parts[12]);
            
            // Swap % for spaces in the channel name
            StringReplace(c.name, "%", " ");
                     
            if (Symbol() == c.symbol) {
            
               if (ShouldDrawOnTimeframe(c.timeframe)) {               
                  Print ("Will render object on " + Symbol());
               
                  RenderChannel(c);
               } else {
                  Print ("Will not render object because of timeframe");
               }
            } else {
               Print ("Will not render object because this is symbol " + Symbol() + " and we want " + c.symbol);
            }
         } 
         else 
         {               
            PrintFormat("Unrecognized object type in line: %s", str);
         }                                 
         
      }           
      
      FileClose(fh);
   } 
}


bool ShouldDrawOnTimeframe(string tf)
{
   int period = Period();   
   
   return 
      (tf == "M1" && period <= PERIOD_M5) ||
      (tf == "M5" && period <= PERIOD_M15) ||
      (tf == "M15" && period <= PERIOD_H1) ||
      (tf == "H1" && period <= PERIOD_H4) ||
      (tf == "H4" && period <= PERIOD_D1) ||
      (tf == "D1" && period >= PERIOD_H4);
}

void RenderChannel(Channel &c)
{  
   long id = ChartID();
   string name = IndicatorID + c.name;
            
   ObjectDelete(id, name);
   
   ObjectCreate(
      name, 
      OBJ_CHANNEL, 
      0,       
      c.startDate, 
      c.startPrice, 
      c.castDate, 
      c.castPrice, 
      c.supportDate, 
      c.supportPrice);
      
   if (ShowObjectDescription(c))
      ObjectSetText(name, c.name);   
   
   ObjectSetInteger(id, name, OBJPROP_COLOR, ColorForChannel(c)); 
   ObjectSetInteger(id, name, OBJPROP_STYLE, StyleForChannel(c)); 
   ObjectSetInteger(id, name, OBJPROP_RAY_RIGHT, true); 
    
}

bool ShowObjectDescription(Channel &c)
{
   return (false);
}

bool IsOwnTimeframe(Channel &c)
{
   int p = Period();
   
   return (
      (c.timeframe == "M1" && p == 1) ||
      (c.timeframe == "M5" && p == 5) ||
      (c.timeframe == "M15" && p == 15) ||
      (c.timeframe == "H1" && p == 60) ||
      (c.timeframe == "H4" && p == 240) ||
      (c.timeframe == "D1" && p == 1440)
   );
}

color ColorForChannel(Channel &c)
{  
   return (IsOwnTimeframe(c) ? clrDarkSlateGray : C'48,48,48');
}

int StyleForChannel(Channel &c)
{
   //return (IsOwnTimeframe(c) ? STYLE_DASHDOTDOT : STYLE_DOT);
   return STYLE_DASHDOTDOT;
}