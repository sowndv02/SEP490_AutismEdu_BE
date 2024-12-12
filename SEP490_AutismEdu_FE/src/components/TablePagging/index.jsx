import { Box, Pagination } from '@mui/material';
import { useEffect, useState } from 'react';

function TablePagging({ pagination, setPagination, setCurrentPage }) {
    const [totalPage, setTotalPage] = useState(0);
    useEffect(() => {
        if (pagination?.total % 10 !== 0) {
            setTotalPage(Math.floor(pagination?.total / 10) + 1);
        } else setTotalPage(Math.floor(pagination?.total / 10));
    }, [pagination])

    const handleChangePage = (event, value) => {
        setCurrentPage(Number(value));
    }
    return (
        <Box sx={{ p: "10px", display: "flex", justifyContent: "space-between" }}>
            <Pagination count={totalPage || 1} page={pagination?.pageNumber || 1} color="primary" onChange={handleChangePage} />
        </Box>
    )
}

export default TablePagging
