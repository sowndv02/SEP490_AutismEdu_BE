import { Box, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography, Stack, TextField, FormControl, InputLabel, MenuItem, Select, Pagination, Paper, IconButton, Button } from '@mui/material';
import React, { useState } from 'react';
import InputAdornment from '@mui/material/InputAdornment';
import SearchIcon from '@mui/icons-material/Search';
import VisibilityIcon from '@mui/icons-material/Visibility';
import services from '~/plugins/services';
import { format } from 'date-fns';
import LoadingComponent from '~/components/LoadingComponent';
import PaymentHistoryDetail from './Modal/PaymentHistoryDetail';
import emptyBook from '~/assets/images/icon/emptybook.gif'
const PaymentHistory = () => {
    const [loading, setLoading] = useState(false);

    const [openDialogDetail, setOpenDialogDetail] = useState(false);
    const [selectedPayment, setSelectedPayment] = useState(null);

    const getTodayDate = () => {
        const today = new Date();
        const year = today.getFullYear();
        const month = String(today.getMonth() + 1).padStart(2, '0');
        const day = String(today.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    };

    const [paymentHistoryList, setPaymentHistoryList] = useState([]);

    const [filters, setFilters] = React.useState({
        search: '',
        startDate: '',
        endDate: getTodayDate(),
        orderBy: 'createdDate',
        sort: 'desc',
        paymentId: 0,
    });

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });

    const handleFilterChange = (key) => (event) => {
        setFilters({
            ...filters,
            [key]: event.target.value,
        });
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    };

    React.useEffect(() => {
        getPaymentHistories();
    }, [filters, pagination.pageNumber]);

    const getPaymentHistories = async () => {
        try {
            setLoading(true);
            await services.PaymentHistoryAPI.getListPaymentHistory(
                (res) => {
                    if (res?.result) {
                        setPaymentHistoryList(res.result);
                        setPagination(res.pagination);
                    }
                },
                (error) => {
                    console.error(error);
                },
                {
                    ...filters,
                    pageNumber: pagination?.pageNumber,
                }
            );
        } catch (error) {
            console.error(error);
        } finally {
            setLoading(false);
        }
    };

    const handleDetail = (item) => {
        setSelectedPayment(item);
        setOpenDialogDetail(true);
    };

    return (
        <Paper variant='elevation' sx={{ p: 3 }}>
            <Typography variant="h4" sx={{ mb: 5 }} textAlign="center">
                Lịch sử giao dịch
            </Typography>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }} spacing={3}>
                <Box sx={{ flex: 3, mr: 3 }}>
                    <TextField
                        fullWidth
                        size="small"
                        label="Tìm kiếm theo nội dung"
                        value={filters.search}
                        onChange={handleFilterChange('search')}
                        InputProps={{
                            endAdornment: (
                                <InputAdornment position="end">
                                    <SearchIcon />
                                </InputAdornment>
                            ),
                        }}
                    />
                </Box>
                <Stack direction="row" spacing={2}>
                    <TextField
                        size="small"
                        type="date"
                        label="Từ ngày"
                        value={filters.startDate}
                        onChange={handleFilterChange('startDate')}
                        InputLabelProps={{ shrink: true }}
                    />
                    <TextField
                        size="small"
                        type="date"
                        label="Đến ngày"
                        value={filters.endDate}
                        onChange={handleFilterChange('endDate')}
                        InputLabelProps={{ shrink: true }}
                    />
                    <FormControl size="small">
                        <InputLabel sx={{ background: 'white', px: 1 }}>Thứ tự</InputLabel>
                        <Select value={filters.sort} onChange={handleFilterChange('sort')}>
                            <MenuItem value="asc">Tăng dần</MenuItem>
                            <MenuItem value="desc">Giảm dần</MenuItem>
                        </Select>
                    </FormControl>
                </Stack>
            </Stack>
            {paymentHistoryList.length !== 0 ? <Box>
                <TableContainer component={Paper} sx={{ boxShadow: 3, borderRadius: 2 }}>
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Mã giao dịch</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Nội dung</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Số tiền</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Ngày giao dịch</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Mã ngân hàng</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {paymentHistoryList.map((item, index) => (
                                <TableRow key={item?.id} hover>
                                    <TableCell>
                                        {index + 1 + (pagination?.pageNumber - 1) * pagination?.pageSize}
                                    </TableCell>
                                    <TableCell>{item?.transactionId || 'N/A'}</TableCell>
                                    <TableCell>{item?.description || 'Không có mô tả'}</TableCell>
                                    <TableCell>
                                        <Button variant="outlined" color="success" size="small">
                                            {item?.amount?.toLocaleString() || 0} đ
                                        </Button>
                                    </TableCell>
                                    <TableCell>
                                        {item?.paymentDate
                                            ? format(new Date(item?.paymentDate), 'HH:mm dd/MM/yyyy')
                                            : 'N/A'}
                                    </TableCell>
                                    <TableCell>{item?.bankTransactionId || 'Không có'}</TableCell>
                                    <TableCell>
                                        <IconButton
                                            color="primary"
                                            onClick={() => handleDetail(item)}
                                        >
                                            <VisibilityIcon />
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>

                <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                    <Pagination
                        count={Math.ceil(pagination.total / pagination.pageSize)}
                        page={pagination.pageNumber}
                        onChange={(e, page) => setPagination((prev) => ({ ...prev, pageNumber: page }))}
                        color="primary"
                    />
                </Stack>
            </Box> : <Box sx={{ textAlign: "center" }}>
                            <img src={emptyBook} style={{ height: "200px" }} />
                            <Typography>Hiện tại không có lịch sử giao dịch nào!</Typography>
                        </Box>}
            <LoadingComponent open={loading} setOpen={setLoading} />
            {openDialogDetail && selectedPayment && <PaymentHistoryDetail open={openDialogDetail} onClose={() => setOpenDialogDetail(false)} paymentHistory={selectedPayment} />}
        </Paper>
    );
};

export default PaymentHistory;