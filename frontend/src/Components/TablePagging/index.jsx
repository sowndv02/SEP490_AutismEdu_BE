import { Box, Pagination, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'

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
            <Typography>Showing {(pagination?.pageNumber - 1) * 10 + 1} to {((pagination?.pageNumber - 1) * 10 + pagination?.currentSize) > pagination?.total ? pagination?.total : ((pagination?.pageNumber - 1) * 10 + pagination?.currentSize)} of {pagination?.total} enteries</Typography>
            <Pagination count={totalPage || 1} page={pagination?.pageNumber || 1} color="primary" onChange={handleChangePage} />
        </Box>
    )
}

export default TablePagging
