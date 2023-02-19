using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShptCrm.Dto
{
    public class ActShpRepository
    {
        public static async Task<IEnumerable<ActShpt>> GetList(MySqlConnection con) 
            => await con.QueryAsync<ActShpt>("SELECT * FROM actshpt WHERE Status=1 OR Status=0 ORDER BY ActDate DESC LIMIT 100");

        public static async Task<ActShpt> Get(MySqlConnection con, int actId)
            => await con.QuerySingleOrDefaultAsync<ActShpt>("SELECT * FROM actshpt WHERE id="+actId);
    }
}
