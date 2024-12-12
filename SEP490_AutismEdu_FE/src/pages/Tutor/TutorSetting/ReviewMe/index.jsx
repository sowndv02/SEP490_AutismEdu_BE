import { Visibility as VisibilityIcon, Delete as DeleteIcon, Star } from '@mui/icons-material';
import {
    Avatar,
    Box,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    FormControl,
    IconButton,
    InputLabel,
    MenuItem,
    Pagination,
    Paper,
    Select,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Tooltip,
    Typography
} from '@mui/material';
import { useState, useEffect } from 'react';
import services from '~/plugins/services';
import SearchIcon from '@mui/icons-material/Search';
import InputAdornment from '@mui/material/InputAdornment';
import LoadingComponent from '~/components/LoadingComponent';
import { format } from 'date-fns';
import ReportIcon from '@mui/icons-material/Report';
import ReportModal from '../../TutorProfile/TutorRating/ReportReview';
import emptyBook from '~/assets/images/icon/emptybook.gif'

function ReviewMe() {
    const [filters, setFilters] = useState({
        // star: '',
        orderBy: 'createdDate',
        sort: 'desc',
    });
    const [reviewList, setReviewList] = useState([]);
    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 5,
        total: 5,
    });
    const [loading, setLoading] = useState(false);
    const [selectedReview, setSelectedReview] = useState('');
    const [detailDialogOpen, setDetailDialogOpen] = useState(false);
    const [openReport, setOpenReport] = useState(false);
    const [currentReport, setCurrentReport] = useState(null);
    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

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


    useEffect(() => {
        fetchReviews();
    }, [filters, pagination.pageNumber]);

    const fetchReviews = async () => {
        try {
            setLoading(true);
            await services.ReviewManagementAPI.getReviewForTutor(
                (res) => {
                    if (res?.result) {
                        setReviewList(res.result);
                        setPagination(res.pagination);
                    }
                },
                (error) => console.error(error),
                {
                    ...filters,
                    pageNumber: pagination.pageNumber,
                    pageSize: pagination.pageSize
                }
            );
        } catch (error) {
            console.error(error);
        } finally {
            setLoading(false);
        }
    };

    const handleOpenDetail = (content) => {
        setSelectedReview(content);
        setDetailDialogOpen(true);
    };

    const handleCloseDetail = () => {
        setDetailDialogOpen(false);
        setSelectedReview('');
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    return (
        <Box sx={{ width: "90%", margin: "auto", mt: "20px", gap: 2 }}>
            <Typography variant='h4' sx={{ mb: 3 }}>Danh sách đánh giá</Typography>

            <Stack direction={'row'} justifyContent={'flex-end'} gap={2} alignItems="center" sx={{ mb: 2 }}>
                {/* <Box sx={{ flexBasis:'200px'}}>
                    <FormControl fullWidth size='small'>
                        <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                        <Select
                            labelId="sort-select-label"
                            value={filters.sort}
                            label="Thứ tự sao"
                            onChange={handleFilterChange('star')}
                            sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                        >
                            <MenuItem value="asc">Tăng dần theo sao</MenuItem>
                            <MenuItem value="desc">Giảm dần theo sao</MenuItem>
                        </Select>
                    </FormControl>
                </Box> */}
                <Box sx={{ flexBasis: '200px' }}>
                    <FormControl fullWidth size='small'>
                        <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                        <Select
                            labelId="sort-select-label"
                            value={filters.sort}
                            label="Thứ tự"
                            onChange={handleFilterChange('sort')}
                            sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                        >
                            <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                            <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                        </Select>
                    </FormControl>
                </Box>
            </Stack>

            {reviewList.length === 0 ? (
                <Box sx={{ textAlign: "center" }}>
                    <img src={emptyBook} style={{ height: "200px" }} />
                    <Typography>Không có đánh giá nào!</Typography>
                </Box>
            ) : (
                <TableContainer component={Paper} sx={{ mt: 3, boxShadow: 3, borderRadius: 2 }}>
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Tên phụ huynh</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Đánh giá</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Nội dung</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                                <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {reviewList.map((review, index) => (
                                <TableRow key={review?.id} hover>
                                    <TableCell>{index + 1 + (pagination.pageNumber - 1) * pagination.pageSize}</TableCell>
                                    <TableCell>
                                        <Tooltip title={review?.parentName || ''} placement="top">
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                <Avatar
                                                    alt={review?.parent?.fullName || 'Phụ huynh'}
                                                    src={review?.parent?.imageUrl || ''}
                                                    sx={{ width: 32, height: 32 }}
                                                />
                                                <Box
                                                    sx={{
                                                        overflow: 'hidden',
                                                        textOverflow: 'ellipsis',
                                                        whiteSpace: 'nowrap',
                                                        maxWidth: '150px',
                                                    }}
                                                >
                                                    {review?.parent?.fullName}
                                                </Box>
                                            </Box>
                                        </Tooltip>
                                    </TableCell>
                                    <TableCell>
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                            <Typography>{review?.rateScore}</Typography>
                                            <Star sx={{ color: '#FFD700', fontSize: '20px' }} />
                                        </Box>
                                    </TableCell>
                                    <TableCell>
                                        <Box sx={{
                                            overflow: 'hidden',
                                            textOverflow: 'ellipsis',
                                            whiteSpace: 'nowrap',
                                            maxWidth: '300px',
                                        }}>
                                            {review?.description}
                                        </Box>
                                    </TableCell>
                                    <TableCell>{format(new Date(review?.createdDate), 'HH:mm dd/MM/yyyy')}</TableCell>
                                    <TableCell>
                                        <IconButton color="primary" onClick={() => handleOpenDetail(review?.description ?? '')}>
                                            <VisibilityIcon />
                                        </IconButton>
                                        <IconButton color="error" onClick={() => { setOpenReport(true); setCurrentReport(review); }}>
                                            <ReportIcon />
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>
            )}

            {reviewList.length !== 0 && (
                <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                    <Pagination
                        count={totalPages}
                        page={pagination.pageNumber}
                        onChange={handlePageChange}
                        color="primary"
                    />
                </Stack>
            )}

            <Dialog open={detailDialogOpen} onClose={handleCloseDetail} maxWidth="sm" fullWidth>
                <DialogTitle textAlign="center">Nội dung đánh giá</DialogTitle>
                <Divider />
                <DialogContent>
                    <Typography variant="subtitle1">
                        {selectedReview || ''}
                    </Typography>
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={handleCloseDetail} variant="outlined" color="primary">
                        Đóng
                    </Button>
                </DialogActions>
            </Dialog>

            <LoadingComponent open={loading} setOpen={setLoading} />
            {openReport && currentReport && <ReportModal open={openReport} setOpen={setOpenReport} currentReport={currentReport} />}
        </Box>
    );
}

export default ReviewMe;
